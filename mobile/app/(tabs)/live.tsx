import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useRef, useState } from 'react';
import {
  Alert, FlatList, Modal, ScrollView, StyleSheet,
  Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  MediaStream, RTCIceCandidate, RTCPeerConnection,
  RTCSessionDescription, RTCView, mediaDevices,
} from 'react-native-webrtc';
import { recipeApi } from '@/services/api';
import { wsService } from '@/services/websocket';
import { Danmaku, LiveQuestion, LiveRoom, Recipe } from '@/types';

const ICE = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

export default function LiveScreen() {
  const [me, setMe] = useState('');
  const [myAvatar, setMyAvatar] = useState('🍳');
  const [rooms, setRooms] = useState<LiveRoom[]>([]);
  const [currentRoom, setCurrentRoom] = useState<LiveRoom | null>(null);
  const [danmakus, setDanmakus] = useState<Danmaku[]>([]);
  const [questions, setQuestions] = useState<LiveQuestion[]>([]);
  const [danmakuInput, setDanmakuInput] = useState('');
  const [questionInput, setQuestionInput] = useState('');
  const [answerTarget, setAnswerTarget] = useState<LiveQuestion | null>(null);
  const [answerText, setAnswerText] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [selectedRecipe, setSelectedRecipe] = useState<Recipe | null>(null);

  const localStream = useRef<MediaStream | null>(null);
  const remoteStream = useRef<MediaStream | null>(null);
  const [localStreamState, setLocalStreamState] = useState<MediaStream | null>(null);
  const [remoteStreamState, setRemoteStreamState] = useState<MediaStream | null>(null);
  const peers = useRef<Map<string, RTCPeerConnection>>(new Map());

  useEffect(() => {
    AsyncStorage.multiGet(['userName', 'userAvatar']).then(([[, n], [, a]]) => {
      if (n) setMe(n);
      if (a) setMyAvatar(a);
    });
    recipeApi.list().then(setRecipes).catch(() => {});

    const unsubs = [
      wsService.on('live_room_list', setRooms),
      wsService.on('live_room_created', (room: LiveRoom) => {
        setRooms(prev => [...prev.filter(r => r.id !== room.id), room]);
      }),
      wsService.on('live_room_updated', (room: LiveRoom) => {
        setRooms(prev => prev.map(r => r.id === room.id ? room : r));
        setCurrentRoom(prev => prev?.id === room.id ? room : prev);
      }),
      wsService.on('live_room_ended', (roomId: string) => {
        setRooms(prev => prev.filter(r => r.id !== roomId));
        setCurrentRoom(prev => prev?.id === roomId ? null : prev);
        cleanupWebRTC();
      }),
      wsService.on('danmaku', (d: Danmaku) => {
        setDanmakus(prev => [...prev.slice(-99), d]);
      }),
      wsService.on('question_asked', (q: LiveQuestion) => {
        setQuestions(prev => [...prev, q]);
      }),
      wsService.on('question_answered', (q: LiveQuestion) => {
        setQuestions(prev => prev.map(old => old.id === q.id ? q : old));
      }),
      // WebRTC 信令
      wsService.on('viewer_wants_stream', (viewerId: string) => sendOffer(viewerId)),
      wsService.on('webrtc_offer', ({ fromId, sdp }: any) => handleOffer(fromId, sdp)),
      wsService.on('webrtc_answer', ({ fromId, sdp }: any) => handleAnswer(fromId, sdp)),
      wsService.on('webrtc_ice', ({ fromId, candidate }: any) => handleIce(fromId, candidate)),
    ];
    return () => { unsubs.forEach(u => u()); cleanupWebRTC(); };
  }, []);

  // ── 摄像头 ──
  const startCamera = async () => {
    try {
      const stream = await mediaDevices.getUserMedia({ video: true, audio: true });
      localStream.current = stream;
      setLocalStreamState(stream);
    } catch (e) {
      Alert.alert('摄像头错误', '无法访问摄像头，请检查权限设置');
    }
  };

  const cleanupWebRTC = () => {
    localStream.current?.getTracks().forEach(t => t.stop());
    localStream.current = null;
    setLocalStreamState(null);
    remoteStream.current = null;
    setRemoteStreamState(null);
    peers.current.forEach(pc => pc.close());
    peers.current.clear();
  };

  // ── WebRTC 信令 ──
  const createPeer = (peerId: string) => {
    const pc = new RTCPeerConnection(ICE) as any;
    peers.current.set(peerId, pc);
    pc.onicecandidate = (e: any) => {
      if (e.candidate) {
        wsService.send('webrtc_ice', { targetId: peerId, candidate: JSON.stringify(e.candidate) });
      }
    };
    pc.ontrack = (e: any) => {
      if (e.streams?.[0]) {
        remoteStream.current = e.streams[0];
        setRemoteStreamState(e.streams[0]);
      }
    };
    return pc;
  };

  const sendOffer = async (viewerId: string) => {
    if (!localStream.current) return;
    const pc = createPeer(viewerId);
    localStream.current.getTracks().forEach((t: any) => pc.addTrack(t, localStream.current!));
    const offer = await pc.createOffer({});
    await pc.setLocalDescription(offer);
    wsService.send('webrtc_offer', { targetId: viewerId, sdp: JSON.stringify(offer) });
  };

  const handleOffer = async (hostId: string, sdpStr: string) => {
    const pc = createPeer(hostId);
    await pc.setRemoteDescription(new RTCSessionDescription(JSON.parse(sdpStr)));
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    wsService.send('webrtc_answer', { targetId: hostId, sdp: JSON.stringify(answer) });
  };

  const handleAnswer = async (viewerId: string, sdpStr: string) => {
    const pc = peers.current.get(viewerId);
    if (pc) await pc.setRemoteDescription(new RTCSessionDescription(JSON.parse(sdpStr)));
  };

  const handleIce = async (peerId: string, candidateStr: string) => {
    const pc = peers.current.get(peerId);
    if (pc) await pc.addIceCandidate(new RTCIceCandidate(JSON.parse(candidateStr)));
  };

  // ── 操作 ──
  const createRoom = async () => {
    if (!selectedRecipe) return;
    wsService.send('create_live_room', { recipeId: selectedRecipe.id, recipeName: selectedRecipe.name });
    setShowCreate(false);
    await startCamera();

    const off = wsService.on('create_live_room_result', (roomId: string) => {
      const room = rooms.find(r => r.id === roomId) ?? {
        id: roomId, name: me + ' 的直播间', hostUserName: me,
        recipeId: selectedRecipe.id, recipeName: selectedRecipe.name,
        currentStep: 0, viewers: [], questions: [], isLive: true, startTime: new Date().toISOString(),
      };
      setCurrentRoom(room as LiveRoom);
      setDanmakus([]); setQuestions([]);
      off();
    });
  };

  const joinRoom = (room: LiveRoom) => {
    wsService.send('join_live_room', { roomId: room.id });
    setCurrentRoom(room);
    setDanmakus([]); setQuestions([]);
  };

  const leaveRoom = () => {
    if (!currentRoom) return;
    wsService.send('leave_live_room', { roomId: currentRoom.id });
    cleanupWebRTC();
    setCurrentRoom(null);
  };

  const endRoom = () => {
    if (!currentRoom) return;
    wsService.send('end_live_room', { roomId: currentRoom.id });
    cleanupWebRTC();
    setCurrentRoom(null);
  };

  const updateStep = (step: number) => {
    if (!currentRoom) return;
    wsService.send('update_live_step', { roomId: currentRoom.id, stepIndex: step });
    setCurrentRoom(prev => prev ? { ...prev, currentStep: step } : null);
  };

  const isHost = currentRoom?.hostUserName === me;

  // ── 大厅 ──
  if (!currentRoom) {
    return (
      <SafeAreaView style={s.safe} edges={['top']}>
        <View style={s.lobbyHeader}>
          <Text style={s.lobbyTitle}>📡 厨房直播间</Text>
          <TouchableOpacity style={s.startBtn} onPress={() => setShowCreate(true)}>
            <Text style={s.startBtnText}>开始直播</Text>
          </TouchableOpacity>
        </View>

        {rooms.filter(r => r.isLive).length === 0 ? (
          <View style={s.empty}><Text style={{ fontSize: 48 }}>📡</Text><Text style={s.emptyText}>暂无直播</Text></View>
        ) : (
          <FlatList
            data={rooms.filter(r => r.isLive)}
            keyExtractor={r => r.id}
            contentContainerStyle={s.list}
            renderItem={({ item: room }) => (
              <View style={s.roomCard}>
                <View style={s.roomInfo}>
                  <View style={s.roomTitleRow}>
                    <Text style={s.liveBadge}>● LIVE</Text>
                    <Text style={s.roomName}>{room.name}</Text>
                  </View>
                  <Text style={s.roomMeta}>🍳 {room.recipeName} · 👥 {room.viewers.length} 人 · 步骤 {room.currentStep + 1}</Text>
                </View>
                <TouchableOpacity style={s.joinBtn} onPress={() => joinRoom(room)}>
                  <Text style={s.joinBtnText}>{room.hostUserName === me ? '返回' : '观看'}</Text>
                </TouchableOpacity>
              </View>
            )}
          />
        )}

        <Modal visible={showCreate} animationType="slide" presentationStyle="pageSheet">
          <SafeAreaView style={s.modal}>
            <View style={s.modalHeader}>
              <Text style={s.modalTitle}>🔴 开始直播</Text>
              <TouchableOpacity onPress={() => setShowCreate(false)}><Text style={s.modalClose}>✕</Text></TouchableOpacity>
            </View>
            <Text style={s.modalSub}>选择要直播的菜谱</Text>
            <FlatList
              data={recipes} keyExtractor={r => r.id}
              contentContainerStyle={{ padding: 16, gap: 8 }}
              renderItem={({ item: r }) => (
                <TouchableOpacity
                  style={[s.recipeItem, selectedRecipe?.id === r.id && s.recipeItemActive]}
                  onPress={() => setSelectedRecipe(r)}>
                  <Text style={s.recipeItemName}>{r.name}</Text>
                  <Text style={s.recipeItemMeta}>{r.cookTimeMinutes} 分钟 · {'⭐'.repeat(r.difficultyLevel)}</Text>
                </TouchableOpacity>
              )}
            />
            <View style={s.modalFooter}>
              <TouchableOpacity
                style={[s.startLiveBtn, !selectedRecipe && s.startLiveBtnDisabled]}
                onPress={createRoom} disabled={!selectedRecipe}>
                <Text style={s.startLiveBtnText}>🔴 开始直播</Text>
              </TouchableOpacity>
            </View>
          </SafeAreaView>
        </Modal>
      </SafeAreaView>
    );
  }

  // ── 直播间内 ──
  return (
    <SafeAreaView style={s.safe} edges={['top']}>
      <View style={s.roomHeader}>
        <Text style={s.liveBadge}>● LIVE</Text>
        <Text style={s.roomHeaderName} numberOfLines={1}>{currentRoom.name}</Text>
        <Text style={s.viewerCount}>👥 {currentRoom.viewers.length}</Text>
        <TouchableOpacity onPress={isHost ? endRoom : leaveRoom} style={s.exitBtn}>
          <Text style={s.exitBtnText}>{isHost ? '结束' : '离开'}</Text>
        </TouchableOpacity>
      </View>

      {/* 视频 */}
      <View style={s.videoContainer}>
        {isHost && localStreamState ? (
          <RTCView streamURL={localStreamState.toURL()} style={s.video} objectFit="cover" mirror />
        ) : !isHost && remoteStreamState ? (
          <RTCView streamURL={remoteStreamState.toURL()} style={s.video} objectFit="cover" />
        ) : (
          <View style={s.videoPlaceholder}>
            <Text style={s.videoPlaceholderText}>{isHost ? '📸 正在启动摄像头...' : '📡 等待主播画面...'}</Text>
          </View>
        )}
      </View>

      {/* 步骤 */}
      <View style={s.stepBar}>
        <Text style={s.stepText} numberOfLines={2}>
          步骤 {currentRoom.currentStep + 1}：{/* TODO: 从菜谱获取步骤内容 */}
        </Text>
        {isHost && (
          <View style={s.stepBtns}>
            <TouchableOpacity disabled={currentRoom.currentStep === 0}
              onPress={() => updateStep(currentRoom.currentStep - 1)} style={s.stepBtn}>
              <Text>◀</Text>
            </TouchableOpacity>
            <TouchableOpacity onPress={() => updateStep(currentRoom.currentStep + 1)} style={s.stepBtn}>
              <Text>▶</Text>
            </TouchableOpacity>
          </View>
        )}
      </View>

      {/* 弹幕 + 提问 */}
      <ScrollView style={s.danmakuList}>
        {danmakus.map((d, i) => (
          <Text key={i} style={s.danmakuItem}><Text style={s.danmakuUser}>{d.avatar} {d.userName}</Text>  {d.content}</Text>
        ))}
      </ScrollView>

      <View style={s.inputArea}>
        <TextInput
          style={s.danmakuInput}
          placeholder={isHost ? '输入弹幕...' : '发弹幕或提问...'}
          value={danmakuInput}
          onChangeText={setDanmakuInput}
          onSubmitEditing={() => {
            if (!danmakuInput.trim()) return;
            wsService.send('send_danmaku', { roomId: currentRoom.id, content: danmakuInput });
            setDanmakuInput('');
          }}
        />
        {!isHost && (
          <TouchableOpacity style={s.questionBtn} onPress={() => {
            if (!danmakuInput.trim()) return;
            wsService.send('ask_question', { roomId: currentRoom.id, content: danmakuInput });
            setDanmakuInput('');
          }}>
            <Text style={s.questionBtnText}>❓</Text>
          </TouchableOpacity>
        )}
      </View>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:              { flex: 1, backgroundColor: '#111' },
  lobbyHeader:       { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: 16, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#eee' },
  lobbyTitle:        { fontSize: 20, fontWeight: 'bold' },
  startBtn:          { backgroundColor: '#ef4444', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 8 },
  startBtnText:      { color: '#fff', fontWeight: '600' },
  list:              { padding: 12, gap: 12 },
  roomCard:          { backgroundColor: '#fff', borderRadius: 14, padding: 16, flexDirection: 'row', alignItems: 'center' },
  roomInfo:          { flex: 1 },
  roomTitleRow:      { flexDirection: 'row', alignItems: 'center', gap: 8 },
  liveBadge:         { backgroundColor: '#ef4444', color: '#fff', fontSize: 11, fontWeight: 'bold', paddingHorizontal: 6, paddingVertical: 2, borderRadius: 4 },
  roomName:          { fontWeight: 'bold', fontSize: 15 },
  roomMeta:          { fontSize: 12, color: '#888', marginTop: 4 },
  joinBtn:           { backgroundColor: '#1b6ec2', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 8 },
  joinBtnText:       { color: '#fff', fontWeight: '600' },
  empty:             { flex: 1, alignItems: 'center', justifyContent: 'center' },
  emptyText:         { color: '#888', marginTop: 8, fontSize: 16 },
  modal:             { flex: 1, backgroundColor: '#fff' },
  modalHeader:       { flexDirection: 'row', justifyContent: 'space-between', padding: 20 },
  modalTitle:        { fontSize: 20, fontWeight: 'bold' },
  modalClose:        { fontSize: 20, color: '#888' },
  modalSub:          { paddingHorizontal: 20, color: '#888', marginBottom: 8 },
  recipeItem:        { borderWidth: 1, borderColor: '#ddd', borderRadius: 12, padding: 14 },
  recipeItemActive:  { borderColor: '#1b6ec2', backgroundColor: '#e8f0fe' },
  recipeItemName:    { fontSize: 16, fontWeight: '600' },
  recipeItemMeta:    { fontSize: 12, color: '#888', marginTop: 2 },
  modalFooter:       { padding: 20, borderTopWidth: 1, borderColor: '#eee' },
  startLiveBtn:      { backgroundColor: '#ef4444', borderRadius: 12, padding: 16, alignItems: 'center' },
  startLiveBtnDisabled: { backgroundColor: '#fca5a5' },
  startLiveBtnText:  { color: '#fff', fontSize: 16, fontWeight: 'bold' },
  roomHeader:        { flexDirection: 'row', alignItems: 'center', padding: 12, gap: 8 },
  roomHeaderName:    { flex: 1, color: '#fff', fontWeight: 'bold' },
  viewerCount:       { color: '#fff', fontSize: 13 },
  exitBtn:           { backgroundColor: 'rgba(255,255,255,0.2)', borderRadius: 16, paddingHorizontal: 12, paddingVertical: 6 },
  exitBtnText:       { color: '#fff', fontSize: 13 },
  videoContainer:    { height: 220, backgroundColor: '#000' },
  video:             { flex: 1 },
  videoPlaceholder:  { flex: 1, alignItems: 'center', justifyContent: 'center' },
  videoPlaceholderText: { color: '#888', fontSize: 16 },
  stepBar:           { flexDirection: 'row', alignItems: 'center', backgroundColor: 'rgba(255,255,255,0.1)', padding: 10, gap: 8 },
  stepText:          { flex: 1, color: '#fff', fontSize: 13 },
  stepBtns:          { flexDirection: 'row', gap: 8 },
  stepBtn:           { backgroundColor: 'rgba(255,255,255,0.2)', borderRadius: 8, padding: 8 },
  danmakuList:       { flex: 1, padding: 8 },
  danmakuItem:       { color: 'rgba(255,255,255,0.8)', fontSize: 13, marginBottom: 4 },
  danmakuUser:       { color: '#60a5fa', fontWeight: '600' },
  inputArea:         { flexDirection: 'row', padding: 8, gap: 8 },
  danmakuInput:      { flex: 1, backgroundColor: 'rgba(255,255,255,0.15)', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 10, color: '#fff' },
  questionBtn:       { backgroundColor: 'rgba(255,255,255,0.2)', borderRadius: 20, paddingHorizontal: 14, justifyContent: 'center' },
  questionBtnText:   { fontSize: 20 },
});
