import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { CacheService } from './cache.service';
import { PagedResult } from '../types/page-result.model';
import { 
  CreateDonYeuCauDto, 
  DonYeuCauDto, 
  DuyetDonYeuCauDto, 
  FilterDonYeuCauDto, 
  LoaiDonYeuCau, 
  ThongKeDonYeuCauDto, 
  TrangThaiDon, 
  UpdateDonYeuCauDto 
} from '../types/don.model';

@Injectable({
  providedIn: 'root'
})
export class DonYeuCauService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(CacheService);
  private readonly baseUrl = `${environment.apiUrl}/DonYeuCaus`;
  private readonly CACHE_PREFIX = 'donyeucau_';

  // ============================================================================
  // CRUD Operations
  // ============================================================================

  /**
   * Lấy danh sách đơn yêu cầu với filter (Giám Đốc và Trưởng Phòng)
   * GET /api/DonYeuCaus
   */
  getAll(filter: FilterDonYeuCauDto): Observable<PagedResult<DonYeuCauDto>> {
    const cacheKey = `${this.CACHE_PREFIX}all_${JSON.stringify(filter)}`;
    
    const cached = this.cache.get<DonYeuCauDto>(cacheKey);
    if (cached) {
      return of(cached);
    }

    let params = new HttpParams()
      .set('pageNumber', (filter.pageNumber || 1).toString())
      .set('pageSize', (filter.pageSize || 10).toString());

    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.loaiDon !== undefined) params = params.set('loaiDon', filter.loaiDon.toString());
    if (filter.trangThai !== undefined) params = params.set('trangThai', filter.trangThai.toString());
    if (filter.nhanVienId) params = params.set('nhanVienId', filter.nhanVienId);
    if (filter.nguoiDuyetId) params = params.set('nguoiDuyetId', filter.nguoiDuyetId);
    if (filter.phongBanId) params = params.set('phongBanId', filter.phongBanId);
    if (filter.tuNgay) params = params.set('tuNgay', this.formatDate(filter.tuNgay));
    if (filter.denNgay) params = params.set('denNgay', this.formatDate(filter.denNgay));

    return this.http.get<PagedResult<DonYeuCauDto>>(this.baseUrl, { params })
      .pipe(tap(result => this.cache.set(cacheKey, result)));
  }

  /**
   * Lấy đơn yêu cầu theo ID
   * GET /api/DonYeuCaus/{id}
   */
  getById(id: string): Observable<DonYeuCauDto> {
    return this.http.get<DonYeuCauDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Tạo đơn yêu cầu mới
   * POST /api/DonYeuCaus
   */
  create(dto: CreateDonYeuCauDto): Observable<DonYeuCauDto> {
    return this.http.post<DonYeuCauDto>(this.baseUrl, dto)
      .pipe(tap(() => this.clearCache()));
  }

  /**
   * Cập nhật đơn yêu cầu (chỉ owner và đơn đang chờ duyệt)
   * PUT /api/DonYeuCaus/{id}
   */
  update(id: string, dto: UpdateDonYeuCauDto): Observable<DonYeuCauDto> {
    return this.http.put<DonYeuCauDto>(`${this.baseUrl}/${id}`, dto)
      .pipe(tap(() => this.clearCache()));
  }

  /**
   * Xóa đơn yêu cầu (Giám Đốc hoặc owner khi đơn chưa duyệt)
   * DELETE /api/DonYeuCaus/{id}
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`)
      .pipe(tap(() => this.clearCache()));
  }

  // ============================================================================
  // My Dons (Đơn của tôi)
  // ============================================================================

  /**
   * Lấy danh sách đơn của tôi
   * GET /api/DonYeuCaus/my-dons
   */
  getMyDons(
    pageNumber: number = 1,
    pageSize: number = 10,
    loaiDon?: LoaiDonYeuCau,
    trangThai?: TrangThaiDon
  ): Observable<PagedResult<DonYeuCauDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (loaiDon !== undefined) params = params.set('loaiDon', loaiDon.toString());
    if (trangThai !== undefined) params = params.set('trangThai', trangThai.toString());

    return this.http.get<PagedResult<DonYeuCauDto>>(`${this.baseUrl}/my-dons`, { params });
  }

  /**
   * Hủy đơn của tôi (chỉ đơn đang chờ duyệt)
   * POST /api/DonYeuCaus/{id}/huy
   */
  huyDon(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/huy`, {})
      .pipe(tap(() => this.clearCache()));
  }

  // ============================================================================
  // Duyệt Đơn (Giám Đốc và Trưởng Phòng)
  // ============================================================================

  /**
   * Lấy danh sách đơn cần duyệt (Giám Đốc và Trưởng Phòng)
   * GET /api/DonYeuCaus/can-duyet
   */
  getDonCanDuyet(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResult<DonYeuCauDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<DonYeuCauDto>>(`${this.baseUrl}/can-duyet`, { params });
  }

  /**
   * Chấp thuận đơn (Giám Đốc và Trưởng Phòng)
   * POST /api/DonYeuCaus/{id}/chap-thuan
   */
  chapThuan(id: string, ghiChu?: string): Observable<DonYeuCauDto> {
    const dto: DuyetDonYeuCauDto = {
      trangThai: TrangThaiDon.DaChapThuan,
      ghiChuNguoiDuyet: ghiChu
    };
    return this.http.post<DonYeuCauDto>(`${this.baseUrl}/${id}/chap-thuan`, dto)
      .pipe(tap(() => this.clearCache()));
  }

  /**
   * Từ chối đơn (Giám Đốc và Trưởng Phòng)
   * POST /api/DonYeuCaus/{id}/tu-choi
   */
  tuChoi(id: string, ghiChu: string): Observable<DonYeuCauDto> {
    const dto: DuyetDonYeuCauDto = {
      trangThai: TrangThaiDon.BiTuChoi,
      ghiChuNguoiDuyet: ghiChu
    };
    return this.http.post<DonYeuCauDto>(`${this.baseUrl}/${id}/tu-choi`, dto)
      .pipe(tap(() => this.clearCache()));
  }

  /**
   * Đếm số đơn đang chờ duyệt (Giám Đốc và Trưởng Phòng)
   * GET /api/DonYeuCaus/count-cho-duyet
   */
  countDonChoDuyet(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.baseUrl}/count-cho-duyet`);
  }

  // ============================================================================
  // Thống kê
  // ============================================================================

  /**
   * Thống kê đơn của tôi
   * GET /api/DonYeuCaus/thong-ke/my-dons
   */
  thongKeMyDons(fromDate?: Date | string, toDate?: Date | string): Observable<ThongKeDonYeuCauDto> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', this.formatDate(fromDate));
    if (toDate) params = params.set('toDate', this.formatDate(toDate));

    return this.http.get<ThongKeDonYeuCauDto>(`${this.baseUrl}/thong-ke/my-dons`, { params });
  }

  /**
   * Thống kê đơn theo phòng ban (Trưởng Phòng)
   * GET /api/DonYeuCaus/thong-ke/phong-ban/{phongBanId}
   */
  thongKePhongBan(
    phongBanId: string, 
    fromDate?: Date | string, 
    toDate?: Date | string
  ): Observable<ThongKeDonYeuCauDto> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', this.formatDate(fromDate));
    if (toDate) params = params.set('toDate', this.formatDate(toDate));

    return this.http.get<ThongKeDonYeuCauDto>(
      `${this.baseUrl}/thong-ke/phong-ban/${phongBanId}`, 
      { params }
    );
  }

  /**
   * Thống kê đơn toàn công ty (chỉ Giám Đốc)
   * GET /api/DonYeuCaus/thong-ke/toan-cong-ty
   */
  thongKeToanCongTy(fromDate?: Date | string, toDate?: Date | string): Observable<ThongKeDonYeuCauDto> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', this.formatDate(fromDate));
    if (toDate) params = params.set('toDate', this.formatDate(toDate));

    return this.http.get<ThongKeDonYeuCauDto>(`${this.baseUrl}/thong-ke/toan-cong-ty`, { params });
  }

  // ============================================================================
  // Private Helper Methods
  // ============================================================================

  /**
   * Format date thành ISO string cho API
   */
  private formatDate(date: Date | string): string {
    if (typeof date === 'string') return date;
    return date.toISOString();
  }

  /**
   * Clear cache với prefix
   */
  private clearCache(): void {
    this.cache.clear(this.CACHE_PREFIX);
  }
}

 