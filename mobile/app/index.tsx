import AsyncStorage from '@react-native-async-storage/async-storage';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import {
  KeyboardAvoidingView, Platform, ScrollView,
  StyleSheet, Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { wsService } from '@/services/websocket';

const AVATARS = ['👨‍🍳', '👩‍🍳', '🧑‍🍳', '🍳', '🥘', '🍜', '🍲', '🥗', '🍱', '🍣'];

export default function LoginScreen() {
  const [name, setName] = useState('');
  const [avatar, setAvatar] = useState(AVATARS[0]);

  useEffect(() => {
    AsyncStorage.multiGet(['userName', 'userAvatar']).then(([[, n], [, a]]) => {
      if (n) { setName(n); router.replace('/(tabs)'); }
      if (a) setAvatar(a);
    });
  }, []);

  const enter = async () => {
    if (!name.trim()) return;
    await AsyncStorage.multiSet([['userName', name.trim()], ['userAvatar', avatar]]);
    wsService.connect(name.trim(), avatar);
    router.replace('/(tabs)');
  };

  return (
    <SafeAreaView style={s.safe}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={s.kav}>
        <ScrollView contentContainerStyle={s.scroll}>
          <Text style={s.emoji}>🍳</Text>
          <Text style={s.title}>厨房协作助手</Text>
          <Text style={s.sub}>让做饭变得更简单、更有趣</Text>

          <View style={s.card}>
            <Text style={s.label}>选择头像</Text>
            <View style={s.avatarRow}>
              {AVATARS.map(a => (
                <TouchableOpacity
                  key={a}
                  style={[s.avatarBtn, avatar === a && s.avatarBtnActive]}
                  onPress={() => setAvatar(a)}>
                  <Text style={s.avatarText}>{a}</Text>
                </TouchableOpacity>
              ))}
            </View>

            <Text style={s.label}>你的昵称</Text>
            <TextInput
              style={s.input}
              placeholder="输入昵称..."
              value={name}
              onChangeText={setName}
              onSubmitEditing={enter}
              autoFocus
            />

            <TouchableOpacity
              style={[s.btn, !name.trim() && s.btnDisabled]}
              onPress={enter}
              disabled={!name.trim()}>
              <Text style={s.btnText}>进入厨房 🍳</Text>
            </TouchableOpacity>

            <Text style={s.hint}>输入昵称即可开始，无需注册</Text>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:         { flex: 1, backgroundColor: '#1b6ec2' },
  kav:          { flex: 1 },
  scroll:       { flexGrow: 1, justifyContent: 'center', padding: 24 },
  emoji:        { fontSize: 64, textAlign: 'center', marginBottom: 8 },
  title:        { fontSize: 28, fontWeight: 'bold', color: '#fff', textAlign: 'center' },
  sub:          { color: 'rgba(255,255,255,0.8)', textAlign: 'center', marginBottom: 32 },
  card:         { backgroundColor: '#fff', borderRadius: 16, padding: 24, shadowColor: '#000', shadowOpacity: 0.15, shadowRadius: 12, elevation: 5 },
  label:        { fontWeight: '600', color: '#333', marginBottom: 8 },
  avatarRow:    { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 20 },
  avatarBtn:    { width: 44, height: 44, borderRadius: 22, borderWidth: 2, borderColor: '#e0e0e0', alignItems: 'center', justifyContent: 'center' },
  avatarBtnActive: { borderColor: '#1b6ec2', backgroundColor: '#e8f0fe' },
  avatarText:   { fontSize: 22 },
  input:        { borderWidth: 1, borderColor: '#ddd', borderRadius: 10, padding: 14, fontSize: 16, marginBottom: 16 },
  btn:          { backgroundColor: '#1b6ec2', borderRadius: 10, padding: 16, alignItems: 'center' },
  btnDisabled:  { backgroundColor: '#b0c4de' },
  btnText:      { color: '#fff', fontSize: 16, fontWeight: '600' },
  hint:         { textAlign: 'center', color: '#999', fontSize: 12, marginTop: 12 },
});
