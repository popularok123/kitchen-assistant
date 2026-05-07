import AsyncStorage from '@react-native-async-storage/async-storage';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

const MENU = [
  { emoji: '📖', title: '菜谱大全', sub: '浏览精选菜谱，开始烹饪', route: '/recipes' },
  { emoji: '💬', title: '厨房聊天室', sub: '与家人朋友实时交流', route: '/chat' },
  { emoji: '👥', title: '协作烹饪', sub: '多人同步，一起做饭', route: '/collaborate' },
  { emoji: '📡', title: '厨房直播间', sub: '开播或观看做饭直播', route: '/live' },
  { emoji: '🛒', title: '购物清单', sub: '管理食材采购清单', route: '/shopping' },
];

export default function HomeScreen() {
  const [name, setName] = useState('');
  const [avatar, setAvatar] = useState('🍳');

  useEffect(() => {
    AsyncStorage.multiGet(['userName', 'userAvatar']).then(([[, n], [, a]]) => {
      if (n) setName(n);
      if (a) setAvatar(a);
    });
  }, []);

  const logout = async () => {
    await AsyncStorage.multiRemove(['userName', 'userAvatar']);
    router.replace('/');
  };

  return (
    <SafeAreaView style={s.safe} edges={['top']}>
      <ScrollView contentContainerStyle={s.scroll}>
        <View style={s.header}>
          <Text style={s.avatar}>{avatar}</Text>
          <Text style={s.welcome}>你好，{name}！</Text>
          <Text style={s.sub}>今天想做什么呢？</Text>
        </View>

        <View style={s.grid}>
          {MENU.map(item => (
            <TouchableOpacity key={item.route} style={s.card} onPress={() => router.push(item.route as any)}>
              <Text style={s.cardEmoji}>{item.emoji}</Text>
              <Text style={s.cardTitle}>{item.title}</Text>
              <Text style={s.cardSub}>{item.sub}</Text>
            </TouchableOpacity>
          ))}
        </View>

        <TouchableOpacity style={s.logout} onPress={logout}>
          <Text style={s.logoutText}>切换账号</Text>
        </TouchableOpacity>
      </ScrollView>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:       { flex: 1, backgroundColor: '#f5f5f5' },
  scroll:     { padding: 16 },
  header:     { alignItems: 'center', marginBottom: 24 },
  avatar:     { fontSize: 56, marginBottom: 8 },
  welcome:    { fontSize: 24, fontWeight: 'bold', color: '#333' },
  sub:        { color: '#888', marginTop: 4 },
  grid:       { flexDirection: 'row', flexWrap: 'wrap', gap: 12 },
  card:       { backgroundColor: '#fff', borderRadius: 16, padding: 20, width: '47%', shadowColor: '#000', shadowOpacity: 0.06, shadowRadius: 8, elevation: 2 },
  cardEmoji:  { fontSize: 36, marginBottom: 8 },
  cardTitle:  { fontSize: 16, fontWeight: 'bold', color: '#222' },
  cardSub:    { fontSize: 12, color: '#888', marginTop: 4 },
  logout:     { alignSelf: 'center', marginTop: 24, paddingVertical: 10, paddingHorizontal: 24, borderRadius: 20, borderWidth: 1, borderColor: '#ccc' },
  logoutText: { color: '#666' },
});
