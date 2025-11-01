import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { CacheService } from './cache.service';
import { ChucVuDto, CreateChucVuDto, UpdateChucVuDto } from '../types/chucvu.model';
import { PagedResult } from '../types/page-result.model';
import { environment } from '../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class ChucvuService {
  private http = inject(HttpClient);
  private cache = inject(CacheService);
  private apiUrl = `${environment.apiUrl}/api/ChucVus`;
  private readonly CACHE_PREFIX = 'chucvu';

  getAll(pageNumber: number = 1, pageSize: number = 10, searchTerm?: string): Observable<PagedResult<ChucVuDto>> {
    const cacheKey = `${this.CACHE_PREFIX}_${pageNumber}_${pageSize}_${searchTerm || ''}`;
    const cached = this.cache.get<ChucVuDto>(cacheKey);
    
    if (cached) {
      return new Observable(observer => {
        observer.next(cached);
        observer.complete();
      });
    }

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<ChucVuDto>>(this.apiUrl, { params }).pipe(
      tap(result => this.cache.set(cacheKey, result))
    );
  }

  getById(id: string): Observable<ChucVuDto> {
    return this.http.get<ChucVuDto>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateChucVuDto): Observable<ChucVuDto> {
    return this.http.post<ChucVuDto>(this.apiUrl, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  update(id: string, dto: UpdateChucVuDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto).pipe(
      tap(() => this.clearCache())
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.clearCache())
    );
  }

  private clearCache(): void {
    this.cache.clear(this.CACHE_PREFIX);
  }
}
