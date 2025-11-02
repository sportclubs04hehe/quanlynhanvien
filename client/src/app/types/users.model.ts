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
  ngaySinh?: Date;
  ngayVaoLam?: Date;
  telegramChatId?: string;
  phongBan?: PhongBanDto;
  chucVu?: ChucVuDto;
  tenQuanLy?: string;
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
