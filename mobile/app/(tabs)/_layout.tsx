import { Tabs } from 'expo-router';
import { Text } from 'react-native';

function Icon({ emoji, focused }: { emoji: string; focused: boolean }) {
  return <Text style={{ fontSize: 22, opacity: focused ? 1 : 0.5 }}>{emoji}</Text>;
}

export default function TabLayout() {
  return (
    <Tabs screenOptions={{
      tabBarActiveTintColor: '#1b6ec2',
      tabBarStyle: { height: 60, paddingBottom: 8 },
      headerStyle: { backgroundColor: '#1b6ec2' },
      headerTintColor: '#fff',
      headerTitleStyle: { fontWeight: 'bold' },
    }}>
      <Tabs.Screen name="index"
        options={{ title: '首页', tabBarIcon: ({ focused }) => <Icon emoji="🏠" focused={focused} /> }} />
      <Tabs.Screen name="recipes"
        options={{ title: '菜谱', tabBarIcon: ({ focused }) => <Icon emoji="📖" focused={focused} /> }} />
      <Tabs.Screen name="chat"
        options={{ title: '聊天', tabBarIcon: ({ focused }) => <Icon emoji="💬" focused={focused} /> }} />
      <Tabs.Screen name="live"
        options={{ title: '直播', tabBarIcon: ({ focused }) => <Icon emoji="📡" focused={focused} /> }} />
      <Tabs.Screen name="shopping"
        options={{ title: '购物', tabBarIcon: ({ focused }) => <Icon emoji="🛒" focused={focused} /> }} />
    </Tabs>
  );
}
