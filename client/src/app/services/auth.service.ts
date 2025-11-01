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
          console.error('Login failed', err);
          return throwError(() => err);
        })
      );
  }

  logout() {
    this._authState.set(null);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.clear();
    }
  }

  refreshToken() {
    // TODO: Implement refresh token khi backend hỗ trợ
    // Hiện tại backend chưa có endpoint refresh token
    console.warn('Refresh token chưa được implement');
    
    // Tạm thời logout khi token hết hạn
    this.logout();
  }
}
