import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { DonYeuCauDto } from '../types/don.model';

/**
 * Service để xuất dữ liệu ra Excel
 * - Excel: Sử dụng xlsx (SheetJS)
 */
@Injectable({
  providedIn: 'root'
})
export class ExportService {

  /**
   * Xuất danh sách đơn yêu cầu ra Excel
   * @param dons - Danh sách đơn cần xuất
   * @param fileName - Tên file (không cần extension)
   */
  exportToExcel(dons: DonYeuCauDto[], fileName: string = 'DanhSachDonYeuCau'): void {
    if (!dons || dons.length === 0) {
      alert('Không có dữ liệu để xuất!');
      return;
    }

    // Chuẩn bị dữ liệu cho Excel
    const data = dons.map((don, index) => ({
      'STT': index + 1,
      'Mã Đơn': don.maDon || '',
      'Nhân Viên': don.tenNhanVien || '',
      'Email': don.emailNhanVien || '',
      'Phòng Ban': don.phongBan?.tenPhongBan || 'N/A',
      'Chức Vụ': don.chucVu?.tenChucVu || 'N/A',
      'Loại Đơn': don.loaiDonText || '',
      'Loại Nghỉ Phép': don.loaiNghiPhepText || '',
      'Lý Do': don.lyDo || '',
      'Từ Ngày': don.ngayBatDau ? this.formatDate(don.ngayBatDau) : '',
      'Đến Ngày': don.ngayKetThuc ? this.formatDate(don.ngayKetThuc) : '',
      'Số Ngày': don.soNgayThucTe ?? don.soNgay ?? '',
      'Số Giờ Làm Thêm': don.soGioLamThem ?? '',
      'Ngày Làm Thêm': don.ngayLamThem ? this.formatDate(don.ngayLamThem) : '',
      'Ngày Đi Muộn': don.ngayDiMuon ? this.formatDate(don.ngayDiMuon) : '',
      'Giờ Dự Kiến Đến': don.gioDuKienDen ? this.formatTime(don.gioDuKienDen) : '',
      'Địa Điểm Công Tác': don.diaDiemCongTac || '',
      'Mục Đích Công Tác': don.mucDichCongTac || '',
      'Trạng Thái': don.trangThaiText || '',
      'Người Duyệt': don.tenNguoiDuyet || '',
      'Ghi Chú Người Duyệt': don.ghiChuNguoiDuyet || '',
      'Ngày Duyệt': don.ngayDuyet ? this.formatDateTime(don.ngayDuyet) : '',
      'Ngày Tạo': don.ngayTao ? this.formatDateTime(don.ngayTao) : '',
      'Ngày Cập Nhật': don.ngayCapNhat ? this.formatDateTime(don.ngayCapNhat) : ''
    }));

    // Tạo worksheet từ JSON data
    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(data);

    // Tự động điều chỉnh độ rộng cột
    const colWidths = [
      { wch: 5 },   // STT
      { wch: 15 },  // Mã Đơn
      { wch: 20 },  // Nhân Viên
      { wch: 25 },  // Email
      { wch: 15 },  // Phòng Ban
      { wch: 15 },  // Chức Vụ
      { wch: 15 },  // Loại Đơn
      { wch: 15 },  // Loại Nghỉ Phép
      { wch: 30 },  // Lý Do
      { wch: 12 },  // Từ Ngày
      { wch: 12 },  // Đến Ngày
      { wch: 10 },  // Số Ngày
      { wch: 12 },  // Số Giờ
      { wch: 12 },  // Ngày Làm Thêm
      { wch: 12 },  // Ngày Đi Muộn
      { wch: 12 },  // Giờ Đến
      { wch: 20 },  // Địa Điểm
      { wch: 25 },  // Mục Đích
      { wch: 15 },  // Trạng Thái
      { wch: 20 },  // Người Duyệt
      { wch: 30 },  // Ghi Chú
      { wch: 18 },  // Ngày Duyệt
      { wch: 18 },  // Ngày Tạo
      { wch: 18 }   // Ngày Cập Nhật
    ];
    ws['!cols'] = colWidths;

    // Tạo workbook
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Đơn Yêu Cầu');

    // Xuất file
    const timestamp = this.getTimestamp();
    XLSX.writeFile(wb, `${fileName}_${timestamp}.xlsx`);
  }

  /**
   * Format Date thành dd/MM/yyyy
   */
  private formatDate(date: Date | string): string {
    const d = new Date(date);
    if (isNaN(d.getTime())) return '';
    
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    
    return `${day}/${month}/${year}`;
  }

  /**
   * Format DateTime thành dd/MM/yyyy HH:mm
   */
  private formatDateTime(date: Date | string): string {
    const d = new Date(date);
    if (isNaN(d.getTime())) return '';
    
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    
    return `${day}/${month}/${year} ${hours}:${minutes}`;
  }

  /**
   * Format Time thành HH:mm
   */
  private formatTime(date: Date | string): string {
    const d = new Date(date);
    if (isNaN(d.getTime())) return '';
    
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    
    return `${hours}:${minutes}`;
  }

  /**
   * Tạo timestamp cho tên file (yyyyMMdd_HHmmss)
   */
  private getTimestamp(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    
    return `${year}${month}${day}_${hours}${minutes}${seconds}`;
  }
}
