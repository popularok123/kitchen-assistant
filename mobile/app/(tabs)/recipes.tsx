import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import {
  ActivityIndicator, FlatList, StyleSheet,
  Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { recipeApi } from '@/services/api';
import { Recipe } from '@/types';

const DIFFICULTIES = [
  { label: '全部', value: '' },
  { label: '⭐ 简单', value: '1' },
  { label: '⭐⭐ 中等', value: '2' },
  { label: '⭐⭐⭐ 较难', value: '3' },
];

function stars(n: number) { return '⭐'.repeat(n); }

export default function RecipesScreen() {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [difficulty, setDifficulty] = useState('');

  const load = async () => {
    setLoading(true);
    try {
      const data = await recipeApi.list(search || undefined, difficulty || undefined);
      setRecipes(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [search, difficulty]);

  return (
    <SafeAreaView style={s.safe} edges={['top']}>
      <View style={s.searchBar}>
        <TextInput
          style={s.input}
          placeholder="搜索菜谱..."
          value={search}
          onChangeText={setSearch}
          returnKeyType="search"
        />
      </View>

      <View style={s.filters}>
        {DIFFICULTIES.map(d => (
          <TouchableOpacity
            key={d.value}
            style={[s.filterBtn, difficulty === d.value && s.filterBtnActive]}
            onPress={() => setDifficulty(d.value)}>
            <Text style={[s.filterText, difficulty === d.value && s.filterTextActive]}>
              {d.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <ActivityIndicator style={{ flex: 1 }} size="large" color="#1b6ec2" />
      ) : (
        <FlatList
          data={recipes}
          keyExtractor={r => r.id}
          contentContainerStyle={s.list}
          renderItem={({ item: r }) => (
            <TouchableOpacity style={s.card} onPress={() => router.push(`/recipe/${r.id}`)}>
              <View style={s.cardTop}>
                <Text style={s.cardName}>{r.name}</Text>
                <Text style={s.diff}>{stars(r.difficultyLevel)}</Text>
              </View>
              <Text style={s.desc} numberOfLines={1}>{r.description}</Text>
              <View style={s.meta}>
                <Text style={s.metaText}>⏱️ {r.cookTimeMinutes} 分钟</Text>
                <Text style={s.metaText}>🥬 {r.ingredients.length} 种食材</Text>
              </View>
            </TouchableOpacity>
          )}
          ListEmptyComponent={
            <View style={s.empty}>
              <Text style={{ fontSize: 48 }}>🍽️</Text>
              <Text style={s.emptyText}>没有找到匹配的菜谱</Text>
            </View>
          }
        />
      )}
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:            { flex: 1, backgroundColor: '#f5f5f5' },
  searchBar:       { padding: 12 },
  input:           { backgroundColor: '#fff', borderRadius: 10, padding: 12, fontSize: 15, borderWidth: 1, borderColor: '#ddd' },
  filters:         { flexDirection: 'row', paddingHorizontal: 12, gap: 8, marginBottom: 8 },
  filterBtn:       { paddingHorizontal: 14, paddingVertical: 6, borderRadius: 20, borderWidth: 1, borderColor: '#ddd', backgroundColor: '#fff' },
  filterBtnActive: { backgroundColor: '#1b6ec2', borderColor: '#1b6ec2' },
  filterText:      { fontSize: 13, color: '#555' },
  filterTextActive:{ color: '#fff' },
  list:            { padding: 12, gap: 12 },
  card:            { backgroundColor: '#fff', borderRadius: 14, padding: 16, shadowColor: '#000', shadowOpacity: 0.06, shadowRadius: 6, elevation: 2 },
  cardTop:         { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  cardName:        { fontSize: 17, fontWeight: 'bold', color: '#222' },
  diff:            { fontSize: 14 },
  desc:            { color: '#777', fontSize: 13, marginBottom: 8 },
  meta:            { flexDirection: 'row', gap: 16 },
  metaText:        { fontSize: 12, color: '#888' },
  empty:           { alignItems: 'center', marginTop: 60 },
  emptyText:       { color: '#aaa', marginTop: 8 },
});
