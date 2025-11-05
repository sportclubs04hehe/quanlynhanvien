import { ChucVuDto } from "./chucvu.model";
import { PhongBanDto } from "./phongban.model";

////////////////////////////////////////////////////////////////////////////
// LE MINH HUY
////////////////////////////////////////////////////////////////////////////

/**
 * Loại đơn yêu cầu
 * Phải khớp chính xác với backend: api/Model/Enums/LoaiDonYeuCau.cs
 */
export enum LoaiDonYeuCau {
  NghiPhep = 'NghiPhep',          // Đơn xin nghỉ phép (có lương)
  LamThemGio = 'LamThemGio',      // Đơn xin làm thêm giờ (overtime)
  DiMuon = 'DiMuon',              // Đơn xin đi muộn
  CongTac = 'CongTac'             // Đơn xin đi công tác
}

/**
 * Trạng thái đơn yêu cầu
 * Phải khớp chính xác với backend: api/Model/Enums/TrangThaiDon.cs
 */
export enum TrangThaiDon {
  DangChoDuyet = 'DangChoDuyet',  // Đơn mới tạo, đang chờ duyệt
  DaChapThuan = 'DaChapThuan',    // Đã được phê duyệt
  BiTuChoi = 'BiTuChoi',          // Bị từ chối
  DaHuy = 'DaHuy'                 // Nhân viên tự hủy đơn
}

////////////////////////////////////////////////////////////////////////////
// DISPLAY NAMES / HELPERS
////////////////////////////////////////////////////////////////////////////

/**
 * Tên hiển thị cho LoaiDonYeuCau
 */
export const LOAI_DON_DISPLAY_NAMES: Record<LoaiDonYeuCau, string> = {
  [LoaiDonYeuCau.NghiPhep]: 'Nghỉ Phép',
  [LoaiDonYeuCau.LamThemGio]: 'Làm Thêm Giờ',
  [LoaiDonYeuCau.DiMuon]: 'Đi Muộn',
  [LoaiDonYeuCau.CongTac]: 'Công Tác'
};

/**
 * Tên hiển thị cho TrangThaiDon
 */
export const TRANG_THAI_DON_DISPLAY_NAMES: Record<TrangThaiDon, string> = {
  [TrangThaiDon.DangChoDuyet]: 'Đang Chờ Duyệt',
  [TrangThaiDon.DaChapThuan]: 'Đã Chấp Thuận',
  [TrangThaiDon.BiTuChoi]: 'Bị Từ Chối',
  [TrangThaiDon.DaHuy]: 'Đã Hủy'
};

/**
 * Helper function để lấy display name của loại đơn
 */
export function getLoaiDonDisplayName(loaiDon: LoaiDonYeuCau): string {
  return LOAI_DON_DISPLAY_NAMES[loaiDon] || 'Unknown';
}

/**
 * Helper function để lấy display name của trạng thái
 */
export function getTrangThaiDonDisplayName(trangThai: TrangThaiDon): string {
  return TRANG_THAI_DON_DISPLAY_NAMES[trangThai] || 'Unknown';
}

/**
 * Kiểm tra đơn có thể chỉnh sửa không (chỉ khi đang chờ duyệt)
 */
export function canEditDon(trangThai: TrangThaiDon): boolean {
  return trangThai === TrangThaiDon.DangChoDuyet;
}

/**
 * Kiểm tra đơn có thể hủy không (chỉ khi đang chờ duyệt)
 */
export function canCancelDon(trangThai: TrangThaiDon): boolean {
  return trangThai === TrangThaiDon.DangChoDuyet;
}

////////////////////////////////////////////////////////////////////////////
// DTOs
////////////////////////////////////////////////////////////////////////////

/**
 * DTO để tạo đơn yêu cầu mới
 * Maps to: api/DTO/CreateDonYeuCauDto.cs
 */
export interface CreateDonYeuCauDto {
  loaiDon: LoaiDonYeuCau;
  lyDo: string;
  
  // Dành cho Nghỉ Phép và Công Tác
  ngayBatDau?: Date | string;
  ngayKetThuc?: Date | string;
  
  // Dành cho Làm Thêm Giờ
  soGioLamThem?: number;
  ngayLamThem?: Date | string;
  
  // Dành cho Đi Muộn
  gioDuKienDen?: Date | string;
  ngayDiMuon?: Date | string;
  
  // Dành cho Công Tác
  diaDiemCongTac?: string;
  mucDichCongTac?: string;
}

/**
 * DTO để cập nhật đơn yêu cầu (chỉ cho đơn đang chờ duyệt)
 * Maps to: api/DTO/UpdateDonYeuCauDto.cs
 */
export interface UpdateDonYeuCauDto {
  lyDo: string;
  
  // Dành cho Nghỉ Phép và Công Tác
  ngayBatDau?: Date | string;
  ngayKetThuc?: Date | string;
  
  // Dành cho Làm Thêm Giờ
  soGioLamThem?: number;
  ngayLamThem?: Date | string;
  
  // Dành cho Đi Muộn
  gioDuKienDen?: Date | string;
  ngayDiMuon?: Date | string;
  
  // Dành cho Công Tác
  diaDiemCongTac?: string;
  mucDichCongTac?: string;
}

/**
 * DTO trả về thông tin đơn yêu cầu
 * Maps to: api/DTO/DonYeuCauDto.cs
 */
export interface DonYeuCauDto {
  id: string;
  loaiDon: LoaiDonYeuCau;
  loaiDonText: string;         // Tên hiển thị
  trangThai: TrangThaiDon;
  trangThaiText: string;        // Tên hiển thị
  
  // Thông tin nhân viên
  nhanVienId: string;
  tenNhanVien: string;
  emailNhanVien?: string;
  phongBan?: PhongBanDto;
  chucVu?: ChucVuDto;
  
  // Thông tin đơn
  lyDo: string;
  ngayBatDau?: Date | string;
  ngayKetThuc?: Date | string;
  soNgay?: number;              // Tính toán số ngày (cho nghỉ phép/công tác)
  
  // Làm thêm giờ
  soGioLamThem?: number;
  ngayLamThem?: Date | string;
  
  // Đi muộn
  gioDuKienDen?: Date | string;
  ngayDiMuon?: Date | string;
  
  // Công tác
  diaDiemCongTac?: string;
  mucDichCongTac?: string;
  
  // Thông tin duyệt
  duocChapThuanBoi?: string;
  tenNguoiDuyet?: string;
  ghiChuNguoiDuyet?: string;
  ngayDuyet?: Date | string;
  
  // Audit
  ngayTao: Date | string;
  ngayCapNhat?: Date | string;
}

/**
 * DTO để duyệt/từ chối đơn
 * Maps to: api/DTO/DuyetDonYeuCauDto.cs
 */
export interface DuyetDonYeuCauDto {
  trangThai: TrangThaiDon;      // DaChapThuan hoặc BiTuChoi
  ghiChuNguoiDuyet?: string;
}

/**
 * DTO để filter/search đơn yêu cầu
 * Maps to: api/DTO/FilterDonYeuCauDto.cs
 */
export interface FilterDonYeuCauDto {
  // Pagination
  pageNumber?: number;
  pageSize?: number;
  
  // Search
  searchTerm?: string;          // Tìm theo tên nhân viên, lý do
  
  // Filter
  loaiDon?: LoaiDonYeuCau;
  trangThai?: TrangThaiDon;
  nhanVienId?: string;          // Lọc theo nhân viên cụ thể
  nguoiDuyetId?: string;        // Lọc theo người duyệt
  phongBanId?: string;          // Lọc theo phòng ban
  
  // Date range
  tuNgay?: Date | string;
  denNgay?: Date | string;
}

/**
 * DTO thống kê đơn yêu cầu
 * Maps to: api/DTO/ThongKeDonYeuCauDto.cs
 */
export interface ThongKeDonYeuCauDto {
  tongSoDon: number;
  dangChoDuyet: number;
  daChapThuan: number;
  biTuChoi: number;
  daHuy: number;
  
  // Thống kê theo loại
  soDonNghiPhep: number;
  soDonLamThemGio: number;
  soDonDiMuon: number;
  soDonCongTac: number;
}
