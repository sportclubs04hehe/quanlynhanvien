export interface ChucVuDto {
  id: string;
  tenChucVu: string;
  level: number;
  soLuongNhanVien: number;
}

export interface CreateChucVuDto {
  tenChucVu: string;
  level: number;
}

export interface UpdateChucVuDto {
  tenChucVu: string;
  level: number;
}
