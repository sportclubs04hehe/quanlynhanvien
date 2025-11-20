import { DonYeuCauDto } from "./don.model";

export interface NghiPhepQuotaDto {
  id: string;
  nhanVienId: string;
  tenNhanVien: string;
  nam: number;
  thang: number;
  soNgayPhepThang: number;
  soNgayDaSuDung: number;
  soNgayPhepConLai: number;
  daVuotQuota: boolean;
  tongSoGioLamThem: number;
  ghiChu?: string;
}

export interface NgayNghiDetailDto {
  ngay: Date;
  donYeuCauId: string;
  maDon: string;
  loaiDon: string;
  loaiNghiPhep?: string;
  soNgay: number;
  lyDo: string;
}

export interface LichNghiCalendarDto {
  nam: number;
  thang: number;
  ngayDaNghi: NgayNghiDetailDto[];
  soNgayNghiTrongThang: number;
  soGioLamThemTrongThang: number;
}

export interface LichNghiDashboardDto {
  quotaThangHienTai?: NghiPhepQuotaDto;
  tongNgayNghiTrongNam: number;
  tongGioLamThemTrongNam: number;
  calendarThangHienTai?: LichNghiCalendarDto;
  donNghiSapToi: DonYeuCauDto[];
  canhBao: string[];
}

export interface ValidateQuotaRequest {
  ngayBatDau: Date;
  ngayKetThuc: Date;
  soNgayNghi: number;
}

export interface UpsertNghiPhepQuotaDto {
  nhanVienId: string;
  nam: number;
  thang: number;
  soNgayPhepThang: number;
  ghiChu?: string;
}
