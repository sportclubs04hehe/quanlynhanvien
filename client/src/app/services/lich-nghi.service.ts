import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
import { environment } from '../../environments/environment.development';
import {
  LichNghiDashboardDto,
  LichNghiCalendarDto,
  NghiPhepQuotaDto,
  ValidateQuotaRequest,
  UpsertNghiPhepQuotaDto
} from '../types/lichnghi.model';
import { CacheService } from './cache.service';

@Injectable({
  providedIn: 'root'
})
export class LichNghiService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(CacheService);
  private readonly baseUrl = `${environment.apiUrl}/NghiPhepQuota`;

  /**
   * Lấy dashboard "Lịch Nghỉ & Công Việc" của nhân viên hiện tại
   * Sử dụng cache vì data phức tạp và ít thay đổi
   */
  getMyDashboard(nam?: number, thang?: number): Observable<LichNghiDashboardDto> {
    let params = new HttpParams();
    if (nam) params = params.set('nam', nam.toString());
    if (thang) params = params.set('thang', thang.toString());

    const cacheKey = `lichnghi-dashboard-${nam || 'current'}-${thang || 'current'}`;
    const cached = this.cache.get<LichNghiDashboardDto>(cacheKey);
    if (cached) {
      return of(cached.items[0]);
    }

    return this.http.get<LichNghiDashboardDto>(`${this.baseUrl}/dashboard`, { params })
      .pipe(
        tap(data => {
          this.cache.set(cacheKey, { items: [data], totalCount: 1, pageNumber: 1, pageSize: 1, totalPages: 1 });
        })
      );
  }

  /**
   * Lấy quota tháng hiện tại của nhân viên
   * Không cache vì cần real-time cho validation
   */
  getMyQuota(nam?: number, thang?: number): Observable<NghiPhepQuotaDto> {
    let params = new HttpParams();
    if (nam) params = params.set('nam', nam.toString());
    if (thang) params = params.set('thang', thang.toString());

    return this.http.get<NghiPhepQuotaDto>(`${this.baseUrl}/my-quota`, { params });
  }

  /**
   * Lấy calendar view của tháng
   * Sử dụng cache vì render phức tạp
   */
  getCalendar(nam: number, thang: number): Observable<LichNghiCalendarDto> {
    const params = new HttpParams()
      .set('nam', nam.toString())
      .set('thang', thang.toString());

    const cacheKey = `lichnghi-calendar-${nam}-${thang}`;
    const cached = this.cache.get<LichNghiCalendarDto>(cacheKey);
    if (cached) {
      return of(cached.items[0]);
    }

    return this.http.get<LichNghiCalendarDto>(`${this.baseUrl}/calendar`, { params })
      .pipe(
        tap(data => {
          this.cache.set(cacheKey, { items: [data], totalCount: 1, pageNumber: 1, pageSize: 1, totalPages: 1 });
        })
      );
  }

  /**
   * Validate quota trước khi tạo đơn
   * Không cache - phải real-time
   */
  validateQuota(request: ValidateQuotaRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/validate`, request);
  }

  // Admin Operations

  /**
   * Lấy danh sách quota của tất cả nhân viên (Giám Đốc)
   * Không cache - admin cần real-time
   */
  getAllQuotas(nam: number, thang: number, phongBanId?: string): Observable<NghiPhepQuotaDto[]> {
    let params = new HttpParams()
      .set('nam', nam.toString())
      .set('thang', thang.toString());
    
    if (phongBanId) params = params.set('phongBanId', phongBanId);

    return this.http.get<NghiPhepQuotaDto[]>(`${this.baseUrl}/all`, { params });
  }

  /**
   * Tạo quota mới (Giám Đốc)
   */
  createQuota(dto: UpsertNghiPhepQuotaDto): Observable<NghiPhepQuotaDto> {
    return this.http.post<NghiPhepQuotaDto>(this.baseUrl, dto)
      .pipe(
        tap(() => this.cache.clear('lichnghi-'))
      );
  }

  /**
   * Cập nhật quota (Giám Đốc)
   */
  updateQuota(quotaId: string, dto: UpsertNghiPhepQuotaDto): Observable<NghiPhepQuotaDto> {
    return this.http.put<NghiPhepQuotaDto>(`${this.baseUrl}/${quotaId}`, dto)
      .pipe(
        tap(() => this.cache.clear('lichnghi-'))
      );
  }

  /**
   * Clear cache khi có thay đổi (dùng sau khi approve/reject đơn)
   */
  clearCache(): void {
    this.cache.clear('lichnghi-');
  }
}
