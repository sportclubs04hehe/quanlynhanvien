import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { LoginRequest, LoginResponse } from '../types/login.model';
import { catchError, Observable, tap, throwError } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly baseUrl = `${environment.apiUrl}/users`; 

  private _authState = signal<LoginResponse | null>(null);
  authState = computed(() => this._authState());

  isLoggedIn = computed(() => !!this._authState()?.accessToken);
  currentUser = computed(() => this._authState()?.user);

  constructor() {
    // Only access localStorage in browser environment
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem('accessToken');
      const userJson = localStorage.getItem('user');
      
      if (token && userJson) {
        try {
          const user = JSON.parse(userJson);
          this._authState.set({
            tokenType: 'Bearer',
            accessToken: token,
            expiresIn: 0,
            refreshToken: '',
            user: user
          });
        } catch (error) {
          // Nếu parse lỗi, clear storage
          localStorage.clear();
        }
      }
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, payload)
      .pipe(
        tap((res) => {
          this._authState.set(res);
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem('accessToken', res.accessToken);
            localStorage.setItem('user', JSON.stringify(res.user));
            // RefreshToken sẽ implement sau
            if (res.refreshToken) {
              localStorage.setItem('refreshToken', res.refreshToken);
            }
          }
        }),
        catchError((err) => {
          console.error('Login failed:', err);
          // Log chi tiết error để debug
          if (err.error) {
            console.log('Error response:', err.error);
          }
          return throwError(() => err);
        })
      );
  }

  logout() {
    // Revoke token trên server trước khi logout
    const refreshToken = isPlatformBrowser(this.platformId)
      ? localStorage.getItem('refreshToken')
      : null;

    if (refreshToken) {
      // Call revoke token (fire and forget - không cần đợi response)
      this.http.post(`${this.baseUrl}/revoke-token`, { refreshToken })
        .subscribe({
          next: () => console.log('Token revoked successfully'),
          error: (err) => console.warn('Failed to revoke token:', err)
        });
    }

    // Clear local state
    this._authState.set(null);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.clear();
    }
  }

  refreshToken(): Observable<LoginResponse> {
    const refreshToken = isPlatformBrowser(this.platformId) 
      ? localStorage.getItem('refreshToken') 
      : null;
    
    const accessToken = this._authState()?.accessToken;

    if (!refreshToken || !accessToken) {
      this.logout();
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<LoginResponse>(`${this.baseUrl}/refresh-token`, {
      accessToken,
      refreshToken
    }).pipe(
      tap((res) => {
        this._authState.set(res);
        if (isPlatformBrowser(this.platformId)) {
          localStorage.setItem('accessToken', res.accessToken);
          localStorage.setItem('user', JSON.stringify(res.user));
          if (res.refreshToken) {
            localStorage.setItem('refreshToken', res.refreshToken);
          }
        }
      }),
      catchError((err) => {
        console.error('Refresh token failed', err);
        this.logout();
        return throwError(() => err);
      })
    );
  }

  revokeToken(): Observable<any> {
    const refreshToken = isPlatformBrowser(this.platformId)
      ? localStorage.getItem('refreshToken')
      : null;

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token to revoke'));
    }

    return this.http.post(`${this.baseUrl}/revoke-token`, { refreshToken })
      .pipe(
        tap(() => {
          this.logout();
        }),
        catchError((err) => {
          console.error('Revoke token failed', err);
          return throwError(() => err);
        })
      );
  }

  /**
   * Revoke TẤT CẢ tokens của user hiện tại
   * Dùng khi: Đổi mật khẩu, phát hiện bất thường
   */
  revokeAllTokens(): Observable<any> {
    return this.http.post(`${this.baseUrl}/revoke-all-tokens`, {})
      .pipe(
        tap(() => {
          this.logout();
        }),
        catchError((err) => {
          console.error('Revoke all tokens failed', err);
          return throwError(() => err);
        })
      );
  }

  /**
   * Lấy danh sách devices đang login (sessions)
   */
  getActiveSessions(): Observable<any[]> {
    // Pass current refresh token để identify current session
    const refreshToken = isPlatformBrowser(this.platformId)
      ? localStorage.getItem('refreshToken')
      : null;

    const options = refreshToken 
      ? { headers: { 'X-Refresh-Token': refreshToken } }
      : {};

    return this.http.get<any[]>(`${this.baseUrl}/active-sessions`, options);
  }

  /**
   * Revoke một session cụ thể (đăng xuất thiết bị khác)
   */
  revokeSession(sessionId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/revoke-session/${sessionId}`, {})
      .pipe(
        catchError((err) => {
          console.error('Revoke session failed', err);
          return throwError(() => err);
        })
      );
  }
}
