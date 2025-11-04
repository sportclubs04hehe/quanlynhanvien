import { ChucVuDto } from "./chucvu.model";
import { PhongBanDto } from "./phongban.model";

export enum NhanVienStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  OnLeave = 'OnLeave'
}

export interface RegisterUserDto {
  email: string;
  password: string;
  tenDayDu: string;
  phoneNumber?: string;
  phongBanId?: string;
  chucVuId?: string;
  quanLyId?: string;
  ngaySinh?: Date;
  ngayVaoLam?: Date;
  telegramChatId?: string;
}

export interface UserDto {
  id: string;
  email: string;
  phoneNumber?: string;
  tenDayDu: string;
  status: NhanVienStatus;
  ngaySinh?: string | Date;  // API trả về string ISO, có thể parse thành Date
  ngayVaoLam?: string | Date; // API trả về string ISO, có thể parse thành Date
  telegramChatId?: string;
  phongBan?: PhongBanDto;
  chucVu?: ChucVuDto;
  tenQuanLy?: string;
  roles?: string[];  // Thêm roles vì API có trả về
}

export interface UpdateUserDto {
  tenDayDu: string;
  phoneNumber?: string;
  phongBanId?: string;
  chucVuId?: string;
  quanLyId?: string;
  ngaySinh?: Date;
  ngayVaoLam?: Date;
  telegramChatId?: string;
  status?: NhanVienStatus;
}
