import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useRef, useState } from 'react';
import {
  FlatList, KeyboardAvoidingView, Platform, StyleSheet,
  Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { wsService } from '@/services/websocket';
import { Message, User } from '@/types';

const QUICK = ['👍 好的', '🔥 加油', '😋 好香', '🙏 帮忙', '✅ 完成'];

export default function ChatScreen() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [onlineUsers, setOnlineUsers] = useState<User[]>([]);
  const [input, setInput] = useState('');
  const [me, setMe] = useState('');
  const [myAvatar, setMyAvatar] = useState('🍳');
  const [privateTarget, setPrivateTarget] = useState('');
  const listRef = useRef<FlatList>(null);

  useEffect(() => {
    AsyncStorage.multiGet(['userName', 'userAvatar']).then(([[, n], [, a]]) => {
      if (n) setMe(n);
      if (a) setMyAvatar(a);
    });

    const unsubs = [
      wsService.on('message_history', (history: Message[]) => {
        setMessages(history);
        scrollBottom();
      }),
      wsService.on('new_message', (msg: Message) => {
        setMessages(prev => {
          const next = [...prev, msg];
          return next.slice(-100);
        });
        scrollBottom();
      }),
      wsService.on('message_recalled', (msgId: string) => {
        setMessages(prev => prev.filter(m => m.id !== msgId));
      }),
      wsService.on('online_users', (users: User[]) => setOnlineUsers(users)),
    ];
    return () => unsubs.forEach(u => u());
  }, []);

  const scrollBottom = () => {
    setTimeout(() => listRef.current?.scrollToEnd({ animated: true }), 100);
  };

  const send = () => {
    if (!input.trim()) return;
    if (privateTarget) {
      wsService.send('send_private', { targetUserName: privateTarget, content: input.trim() });
    } else {
      wsService.send('send_message', { content: input.trim() });
    }
    setInput('');
    setPrivateTarget('');
  };

  const recall = (msgId: string) => {
    wsService.send('recall_message', { messageId: msgId });
  };

  const renderMsg = ({ item: m }: { item: Message }) => {
    const isMe = m.userName === me;
    if (m.type === 'system') {
      return (
        <View style={s.sysRow}>
          <Text style={s.sysText}>{m.content}</Text>
        </View>
      );
    }
    return (
      <View style={[s.msgRow, isMe && s.msgRowMe]}>
        {!isMe && <Text style={s.msgAvatar}>{m.avatar}</Text>}
        <View style={[s.bubble, isMe ? s.bubbleMe : s.bubbleOther,
          m.type === 'private' && s.bubblePrivate]}>
          {m.type === 'private' && (
            <Text style={s.privateLabel}>
              🔒 {isMe ? `私信给 ${m.targetUserName}` : `${m.userName} 私信给你`}
            </Text>
          )}
          <Text style={[s.bubbleText, isMe && s.bubbleTextMe]}>{m.content}</Text>
          {isMe && (
            <TouchableOpacity onPress={() => recall(m.id)} style={s.recallBtn}>
              <Text style={s.recallText}>撤回</Text>
            </TouchableOpacity>
          )}
        </View>
        {isMe && <Text style={s.msgAvatar}>{myAvatar}</Text>}
      </View>
    );
  };

  return (
    <SafeAreaView style={s.safe} edges={['top']}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        {/* 在线用户 */}
        <View style={s.onlineBar}>
          <Text style={s.onlineLabel}>在线 {onlineUsers.length} 人：</Text>
          {onlineUsers.slice(0, 5).map(u => (
            <TouchableOpacity key={u.id} onPress={() => setPrivateTarget(u.name === me ? '' : u.name)}>
              <Text style={[s.onlineUser, u.name === me && s.onlineMe]}>{u.avatar} {u.name}</Text>
            </TouchableOpacity>
          ))}
        </View>

        {privateTarget !== '' && (
          <View style={s.privateBar}>
            <Text style={s.privateBarText}>🔒 私信给 {privateTarget}</Text>
            <TouchableOpacity onPress={() => setPrivateTarget('')}>
              <Text style={s.cancelPrivate}>取消</Text>
            </TouchableOpacity>
          </View>
        )}

        <FlatList
          ref={listRef}
          data={messages}
          keyExtractor={m => m.id}
          contentContainerStyle={s.list}
          renderItem={renderMsg}
          onContentSizeChange={scrollBottom}
        />

        {/* 快捷回复 */}
        <View style={s.quickRow}>
          {QUICK.map(q => (
            <TouchableOpacity key={q} style={s.quickBtn} onPress={() => wsService.send('send_message', { content: q })}>
              <Text style={s.quickText}>{q}</Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* 输入框 */}
        <View style={s.inputRow}>
          <TextInput
            style={s.input}
            placeholder={privateTarget ? `私信 ${privateTarget}...` : '输入消息...'}
            value={input}
            onChangeText={setInput}
            onSubmitEditing={send}
            returnKeyType="send"
          />
          <TouchableOpacity style={[s.sendBtn, !input.trim() && s.sendBtnDisabled]} onPress={send} disabled={!input.trim()}>
            <Text style={s.sendText}>{privateTarget ? '🔒' : '发送'}</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:          { flex: 1, backgroundColor: '#f5f5f5' },
  onlineBar:     { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', padding: 8, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#eee' },
  onlineLabel:   { fontSize: 12, color: '#888' },
  onlineUser:    { fontSize: 12, color: '#1b6ec2', marginRight: 8 },
  onlineMe:      { fontWeight: 'bold' },
  list:          { padding: 12, gap: 8 },
  sysRow:        { alignItems: 'center', marginVertical: 4 },
  sysText:       { fontSize: 12, color: '#aaa', backgroundColor: '#f0f0f0', paddingHorizontal: 12, paddingVertical: 4, borderRadius: 12 },
  msgRow:        { flexDirection: 'row', alignItems: 'flex-end', gap: 8 },
  msgRowMe:      { justifyContent: 'flex-end' },
  msgAvatar:     { fontSize: 28 },
  bubble:        { maxWidth: '70%', borderRadius: 14, padding: 12 },
  bubbleOther:   { backgroundColor: '#fff' },
  bubbleMe:      { backgroundColor: '#1b6ec2' },
  bubblePrivate: { borderWidth: 1.5, borderColor: '#ffc107' },
  bubbleText:    { fontSize: 15, color: '#333' },
  bubbleTextMe:  { color: '#fff' },
  privateLabel:  { fontSize: 11, color: '#f59e0b', marginBottom: 4 },
  recallBtn:     { marginTop: 4, alignSelf: 'flex-end' },
  recallText:    { fontSize: 11, color: 'rgba(255,255,255,0.6)' },
  privateBar:    { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', backgroundColor: '#fef3c7', paddingHorizontal: 16, paddingVertical: 8 },
  privateBarText:{ fontSize: 13, color: '#92400e' },
  cancelPrivate: { color: '#1b6ec2', fontSize: 13 },
  quickRow:      { flexDirection: 'row', flexWrap: 'wrap', gap: 6, padding: 8, backgroundColor: '#fff', borderTopWidth: 1, borderColor: '#eee' },
  quickBtn:      { borderWidth: 1, borderColor: '#ddd', borderRadius: 16, paddingHorizontal: 10, paddingVertical: 5 },
  quickText:     { fontSize: 12, color: '#555' },
  inputRow:      { flexDirection: 'row', padding: 8, backgroundColor: '#fff', gap: 8 },
  input:         { flex: 1, backgroundColor: '#f5f5f5', borderRadius: 22, paddingHorizontal: 16, paddingVertical: 10, fontSize: 15 },
  sendBtn:       { backgroundColor: '#1b6ec2', borderRadius: 22, paddingHorizontal: 20, justifyContent: 'center' },
  sendBtnDisabled: { backgroundColor: '#b0c4de' },
  sendText:      { color: '#fff', fontWeight: '600' },
});
