export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserInfo {
  id: string;
  email: string;
  phoneNumber?: string;
  tenDayDu: string;
  status: number;
  ngaySinh?: string;
  ngayVaoLam?: string;
  telegramChatId?: string;
  phongBan?: any;
  chucVu?: any;
  tenQuanLy?: string;
  roles: string[];
}

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
  user: UserInfo;
}