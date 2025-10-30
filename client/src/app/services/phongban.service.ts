import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { PagedResult } from '../types/page-result.model';
import { CreatePhongBanDto, PhongBanDto, UpdatePhongBanDto } from '../types/phongban.model';
import { CacheService } from './cache.service';

@Injectable({
  providedIn: 'root'
})
export class PhongbanService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(CacheService);
  private readonly baseUrl = `${environment.apiUrl}/api/PhongBans`;
  private readonly CACHE_PREFIX = 'phongban_';

  getAll(pageNumber: number = 1, pageSize: number = 10, searchTerm?: string): Observable<PagedResult<PhongBanDto>> {
    const cacheKey = `${this.CACHE_PREFIX}${pageNumber}_${pageSize}_${searchTerm || ''}`;
    
    // Kiểm tra cache
    const cached = this.cache.get<PhongBanDto>(cacheKey);
    if (cached) {
      return of(cached);
    }

    // Gọi API nếu không có cache
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<PhongBanDto>>(this.baseUrl, { params })
      .pipe(
        tap(result => this.cache.set(cacheKey, result))
      );
  }

  getById(id: string): Observable<PhongBanDto> {
    return this.http.get<PhongBanDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreatePhongBanDto): Observable<PhongBanDto> {
    return this.http.post<PhongBanDto>(this.baseUrl, dto)
      .pipe(
        tap(() => this.clearCache())
      );
  }

  update(id: string, dto: UpdatePhongBanDto): Observable<PhongBanDto> {
    return this.http.put<PhongBanDto>(`${this.baseUrl}/${id}`, dto)
      .pipe(
        tap(() => this.clearCache())
      );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`)
      .pipe(
        tap(() => this.clearCache())
      );
  }

  private clearCache(): void {
    this.cache.clear(this.CACHE_PREFIX);
  }
}

