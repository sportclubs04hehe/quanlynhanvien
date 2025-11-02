export interface Session {
  id: string;
  token: string;
  createdAt: Date;
  expiresAt: Date;
  createdByIp: string;
  isActive: boolean;
  isCurrentSession: boolean;
}