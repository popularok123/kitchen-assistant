import { Platform } from 'react-native';

// 开发时指向本机 IP，生产时改为真实服务器
const DEV_HOST = Platform.OS === 'android' ? '10.0.2.2' : '172.20.10.2';

export const API_BASE = `http://${DEV_HOST}:8080`;
export const WS_URL   = `ws://${DEV_HOST}:8080/ws`;
