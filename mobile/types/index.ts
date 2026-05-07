export interface User {
  id: string;
  name: string;
  avatar: string;
  joinedAt: string;
  isOnline: boolean;
}

export type MessageType = 'normal' | 'system' | 'private' | 'progress';

export interface Message {
  id: string;
  userName: string;
  avatar: string;
  content: string;
  type: MessageType;
  targetUserName?: string;
  timestamp: string;
}

export interface CookingStep {
  order: number;
  instruction: string;
  durationSeconds: number;
}

export interface Recipe {
  id: string;
  name: string;
  description: string;
  cookTimeMinutes: number;
  difficultyLevel: number;
  ingredients: string[];
  steps: CookingStep[];
  tips: string[];
  isCustom?: boolean;
}

export interface ShoppingItem {
  id: string;
  name: string;
  quantity: string;
  category: string;
  isPurchased: boolean;
  addedAt: string;
  purchasedAt?: string;
}

export type TaskStatus = 'pending' | 'inProgress' | 'completed';

export interface TaskAssignment {
  id: string;
  stepIndex: number;
  stepDescription: string;
  assignedUserName: string;
  status: TaskStatus;
}

export type SessionStatus = 'preparing' | 'cooking' | 'completed';

export interface SessionParticipant {
  userName: string;
  avatar: string;
  isHost: boolean;
}

export interface CookingSession {
  id: string;
  name: string;
  recipeId: string;
  recipeName: string;
  hostUserName: string;
  participants: SessionParticipant[];
  taskAssignments: TaskAssignment[];
  currentStep: number;
  status: SessionStatus;
  startTime: string;
}

export interface LiveQuestion {
  id: string;
  askUserName: string;
  content: string;
  answer?: string;
  isAnswered: boolean;
  askedAt: string;
}

export interface Danmaku {
  roomId: string;
  userName: string;
  avatar: string;
  content: string;
  timestamp: string;
}

export interface LiveViewer {
  userName: string;
  avatar: string;
}

export interface LiveRoom {
  id: string;
  name: string;
  hostUserName: string;
  recipeId: string;
  recipeName: string;
  currentStep: number;
  viewers: LiveViewer[];
  questions: LiveQuestion[];
  isLive: boolean;
  startTime: string;
}

export interface WSMessage {
  type: string;
  payload: any;
}
