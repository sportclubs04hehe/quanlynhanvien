import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { CacheService } from './cache.service';
import { PagedResult } from '../types/page-result.model';
import { RegisterUserDto, UpdateUserDto, UserDto } from '../types/users.model';

@Injectable({
  providedIn: 'root'
})
export class QuanlynhanvienService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(CacheService);
  private readonly baseUrl = `${environment.apiUrl}/api/Users`;
  private readonly CACHE_PREFIX = 'user_';

  getAll(pageNumber: number = 1, pageSize: number = 10, searchTerm?: string): Observable<PagedResult<UserDto>> {
    const cacheKey = `${this.CACHE_PREFIX}${pageNumber}_${pageSize}_${searchTerm || ''}`;
    
    const cached = this.cache.get<UserDto>(cacheKey);
    if (cached) {
      return of(cached);
    }

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<UserDto>>(this.baseUrl, { params })
      .pipe(tap(result => this.cache.set(cacheKey, result)));
  }

  getById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.baseUrl}/${id}`);
  }

  register(dto: RegisterUserDto): Observable<UserDto> {
    return this.http.post<UserDto>(`${this.baseUrl}/register`, dto)
      .pipe(tap(() => this.clearCache()));
  }

  update(id: string, dto: UpdateUserDto): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/${id}`, dto)
      .pipe(tap(() => this.clearCache()));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`)
      .pipe(tap(() => this.clearCache()));
  }

  private clearCache(): void {
    this.cache.clear(this.CACHE_PREFIX);
  }
}
