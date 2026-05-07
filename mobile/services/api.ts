import { API_BASE } from '@/constants/config';
import { Recipe } from '@/types';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const recipeApi = {
  list: (q?: string, difficulty?: string) => {
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (difficulty) params.set('difficulty', difficulty);
    const qs = params.toString();
    return request<Recipe[]>(`/api/recipes${qs ? '?' + qs : ''}`);
  },
  get: (id: string) => request<Recipe>(`/api/recipes/${id}`),
  create: (recipe: Partial<Recipe>) =>
    request<Recipe>('/api/recipes', { method: 'POST', body: JSON.stringify(recipe) }),
  delete: (id: string) =>
    request<void>(`/api/recipes/${id}`, { method: 'DELETE' }),
};
