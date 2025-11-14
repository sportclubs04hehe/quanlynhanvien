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
  role?: string;  
  tenDayDu: string;
  phoneNumber?: string;
  phongBanId?: string;
  chucVuId?: string;
  quanLyId?: string;
  ngaySinh?: Date;
  ngayVaoLam?: Date;
}

export interface UserDto {
  id: string;
  email: string;
  phoneNumber?: string;
  tenDayDu: string;
  status: NhanVienStatus;
  ngaySinh?: string | Date;  // API trả về string ISO, có thể parse thành Date
  ngayVaoLam?: string | Date; // API trả về string ISO, có thể parse thành Date
  
  // IDs
  phongBanId?: string;
  chucVuId?: string;
  quanLyId?: string;
  
  // Related objects
  phongBan?: PhongBanDto;
  chucVu?: ChucVuDto;
  tenQuanLy?: string;
  
  roles?: string[]; 
}

export interface UpdateUserDto {
  tenDayDu: string;
  phoneNumber?: string;
  phongBanId?: string;
  chucVuId?: string;
  quanLyId?: string;
  ngaySinh?: Date;
  ngayVaoLam?: Date;
  status?: NhanVienStatus;
  role?: string;  // Role để cập nhật (chỉ Giám Đốc mới được đổi)
}
