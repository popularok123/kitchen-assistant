import { WS_URL } from '@/constants/config';
import { WSMessage } from '@/types';

type Handler = (payload: any) => void;

class WebSocketService {
  private ws: WebSocket | null = null;
  private handlers = new Map<string, Handler[]>();
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private userName = '';
  private avatar = '';

  connect(userName: string, avatar: string) {
    this.userName = userName;
    this.avatar = avatar;
    this._connect();
  }

  private _connect() {
    this.ws = new WebSocket(WS_URL);

    this.ws.onopen = () => {
      console.log('WebSocket connected');
      this.send('join', { name: this.userName, avatar: this.avatar });
    };

    this.ws.onmessage = (e) => {
      try {
        const msg: WSMessage = JSON.parse(e.data);
        const handlers = this.handlers.get(msg.type) || [];
        handlers.forEach(h => h(msg.payload));
      } catch {}
    };

    this.ws.onclose = () => {
      console.log('WebSocket disconnected, reconnecting in 3s...');
      this.reconnectTimer = setTimeout(() => this._connect(), 3000);
    };

    this.ws.onerror = (e) => console.error('WebSocket error', e);
  }

  disconnect() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.ws?.close();
    this.ws = null;
  }

  send(type: string, payload: any) {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({ type, payload }));
    }
  }

  on(type: string, handler: Handler) {
    const list = this.handlers.get(type) ?? [];
    list.push(handler);
    this.handlers.set(type, list);
    return () => this.off(type, handler);
  }

  off(type: string, handler: Handler) {
    const list = (this.handlers.get(type) ?? []).filter(h => h !== handler);
    this.handlers.set(type, list);
  }

  get isConnected() {
    return this.ws?.readyState === WebSocket.OPEN;
  }
}

export const wsService = new WebSocketService();
