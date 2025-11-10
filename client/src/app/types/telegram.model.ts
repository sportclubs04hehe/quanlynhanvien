// Response từ API khi tạo deep link
export interface TelegramLinkResponse {
  deepLink: string;
  token: string;
  expiresAt: string; // ISO datetime
  expiresInSeconds: number;
}

// Response từ API kiểm tra trạng thái liên kết
export interface TelegramLinkStatus {
  isLinked: boolean;
  chatId?: string;
  hasPendingToken: boolean;
  pendingTokenExpiresAt?: string;
  pendingTokenExpiresInSeconds: number;
}

// Response từ API verify token
export interface TelegramTokenVerification {
  valid: boolean;
  message: string;
  nhanVien?: string;
  expiresAt?: string;
  expiresInSeconds?: number;
  usedAt?: string;
}

// Response chung cho các endpoint
export interface ApiResponse<T = any> {
  message?: string;
  data?: T;
}
