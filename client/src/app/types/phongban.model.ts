export interface CreatePhongBanDto {
  tenPhongBan: string;
  moTa?: string;
}

export interface UpdatePhongBanDto {
  tenPhongBan: string;
  moTa?: string;
}

export interface PhongBanDto {
  id: string;
  tenPhongBan: string;
  moTa?: string;
  soLuongNhanVien: number;
}


