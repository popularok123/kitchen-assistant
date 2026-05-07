import { useEffect, useState } from 'react';
import {
  Alert, FlatList, SectionList, StyleSheet,
  Text, TextInput, TouchableOpacity, View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { recipeApi } from '@/services/api';
import { Recipe, ShoppingItem } from '@/types';
import { uuid } from '@/utils/uuid';

function categorize(name: string): string {
  if (/菜|瓜|椒|葱|姜|蒜|番茄|白菜|菇|茄/.test(name)) return '🥬 蔬菜';
  if (/肉|鸡|鸭|鱼|牛|猪|虾|排|骨/.test(name)) return '🥩 肉类';
  if (/盐|糖|油|酱|醋|料|粉/.test(name)) return '🧂 调料';
  if (/蛋|奶/.test(name)) return '🥚 蛋奶';
  return '📦 其他';
}

export default function ShoppingScreen() {
  const [items, setItems] = useState<ShoppingItem[]>([]);
  const [newName, setNewName] = useState('');
  const [newQty, setNewQty] = useState('');
  const [recipes, setRecipes] = useState<Recipe[]>([]);

  useEffect(() => { recipeApi.list().then(setRecipes).catch(() => {}); }, []);

  const add = () => {
    if (!newName.trim()) return;
    const existing = items.find(i => i.name.toLowerCase() === newName.trim().toLowerCase());
    if (existing) {
      setItems(prev => prev.map(i => i.id === existing.id ? { ...i, quantity: newQty } : i));
    } else {
      setItems(prev => [...prev, {
        id: uuid(), name: newName.trim(), quantity: newQty,
        category: categorize(newName), isPurchased: false, addedAt: new Date().toISOString(),
      }]);
    }
    setNewName(''); setNewQty('');
  };

  const toggle = (id: string) => setItems(prev => prev.map(i =>
    i.id === id ? { ...i, isPurchased: !i.isPurchased, purchasedAt: !i.isPurchased ? new Date().toISOString() : undefined } : i
  ));

  const remove = (id: string) => setItems(prev => prev.filter(i => i.id !== id));

  const addFromRecipe = (recipe: Recipe) => {
    const newItems = recipe.ingredients.map(ing => {
      const parts = ing.split(' ');
      const name = parts[0];
      const qty = parts.slice(1).join(' ');
      return { id: uuid(), name, quantity: qty, category: categorize(name), isPurchased: false, addedAt: new Date().toISOString() } as ShoppingItem;
    });
    setItems(prev => {
      const map = new Map(prev.map(i => [i.name.toLowerCase(), i]));
      newItems.forEach(ni => { if (!map.has(ni.name.toLowerCase())) map.set(ni.name.toLowerCase(), ni); });
      return Array.from(map.values());
    });
  };

  const sections = Object.entries(
    items.reduce((acc, i) => { (acc[i.category] = acc[i.category] || []).push(i); return acc; }, {} as Record<string, ShoppingItem[]>)
  ).map(([title, data]) => ({ title, data }));

  const completed = items.filter(i => i.isPurchased).length;
  const progress = items.length > 0 ? completed / items.length : 0;

  return (
    <SafeAreaView style={s.safe} edges={['top']}>
      <View style={s.header}>
        <View style={s.progressBar}>
          <View style={[s.progressFill, { width: `${progress * 100}%` }]} />
        </View>
        <Text style={s.progressText}>{completed} / {items.length} 已购</Text>
      </View>

      <View style={s.addRow}>
        <TextInput style={s.nameInput} placeholder="添加食材..." value={newName} onChangeText={setNewName} onSubmitEditing={add} />
        <TextInput style={s.qtyInput} placeholder="数量" value={newQty} onChangeText={setNewQty} onSubmitEditing={add} />
        <TouchableOpacity style={s.addBtn} onPress={add} disabled={!newName.trim()}>
          <Text style={s.addBtnText}>➕</Text>
        </TouchableOpacity>
      </View>

      <View style={s.recipeRow}>
        <Text style={s.recipeLabel}>从菜谱添加：</Text>
        <FlatList
          horizontal data={recipes} keyExtractor={r => r.id}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item: r }) => (
            <TouchableOpacity style={s.recipeChip} onPress={() => addFromRecipe(r)}>
              <Text style={s.recipeChipText}>{r.name}</Text>
            </TouchableOpacity>
          )}
        />
      </View>

      {items.length === 0 ? (
        <View style={s.empty}><Text style={{ fontSize: 48 }}>🛒</Text><Text style={s.emptyText}>购物清单还是空的</Text></View>
      ) : (
        <SectionList
          sections={sections}
          keyExtractor={i => i.id}
          contentContainerStyle={s.list}
          renderSectionHeader={({ section }) => (
            <Text style={s.sectionTitle}>{section.title}</Text>
          )}
          renderItem={({ item }) => (
            <View style={[s.item, item.isPurchased && s.itemDone]}>
              <TouchableOpacity style={s.check} onPress={() => toggle(item.id)}>
                <Text style={s.checkText}>{item.isPurchased ? '✅' : '⬜'}</Text>
              </TouchableOpacity>
              <Text style={[s.itemName, item.isPurchased && s.itemNameDone]}>{item.name}</Text>
              {!!item.quantity && <Text style={s.qty}>({item.quantity})</Text>}
              <TouchableOpacity onPress={() => remove(item.id)}>
                <Text style={s.del}>🗑️</Text>
              </TouchableOpacity>
            </View>
          )}
        />
      )}

      {items.some(i => i.isPurchased) && (
        <TouchableOpacity style={s.clearBtn} onPress={() => setItems(prev => prev.filter(i => !i.isPurchased))}>
          <Text style={s.clearText}>清除已购</Text>
        </TouchableOpacity>
      )}
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  safe:         { flex: 1, backgroundColor: '#f5f5f5' },
  header:       { padding: 12, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#eee' },
  progressBar:  { height: 6, backgroundColor: '#e0e0e0', borderRadius: 3, marginBottom: 4 },
  progressFill: { height: 6, backgroundColor: '#22c55e', borderRadius: 3 },
  progressText: { fontSize: 12, color: '#888', textAlign: 'right' },
  addRow:       { flexDirection: 'row', padding: 12, gap: 8, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#eee' },
  nameInput:    { flex: 1, backgroundColor: '#f5f5f5', borderRadius: 10, paddingHorizontal: 12, paddingVertical: 10 },
  qtyInput:     { width: 80, backgroundColor: '#f5f5f5', borderRadius: 10, paddingHorizontal: 8, paddingVertical: 10 },
  addBtn:       { backgroundColor: '#1b6ec2', borderRadius: 10, paddingHorizontal: 14, justifyContent: 'center' },
  addBtnText:   { fontSize: 18 },
  recipeRow:    { flexDirection: 'row', alignItems: 'center', padding: 12, gap: 8 },
  recipeLabel:  { fontSize: 13, color: '#888', flexShrink: 0 },
  recipeChip:   { backgroundColor: '#e8f0fe', borderRadius: 16, paddingHorizontal: 12, paddingVertical: 6, marginRight: 8 },
  recipeChipText: { fontSize: 13, color: '#1b6ec2' },
  list:         { padding: 12 },
  sectionTitle: { fontWeight: '600', color: '#555', marginTop: 12, marginBottom: 4 },
  item:         { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff', borderRadius: 10, padding: 12, marginBottom: 6, gap: 10 },
  itemDone:     { backgroundColor: '#f0fdf4' },
  check:        { width: 28 },
  checkText:    { fontSize: 20 },
  itemName:     { flex: 1, fontSize: 15 },
  itemNameDone: { textDecorationLine: 'line-through', color: '#aaa' },
  qty:          { fontSize: 13, color: '#888' },
  del:          { fontSize: 18 },
  empty:        { alignItems: 'center', marginTop: 80 },
  emptyText:    { color: '#aaa', marginTop: 8 },
  clearBtn:     { margin: 12, backgroundColor: '#f0f0f0', borderRadius: 10, padding: 14, alignItems: 'center' },
  clearText:    { color: '#666' },
});
