import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

export default function RootLayout() {
  return (
    <>
      <StatusBar style="light" />
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="index" />
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="recipe/[id]" options={{ headerShown: true, title: '菜谱详情', headerBackTitle: '返回' }} />
      </Stack>
    </>
  );
}
