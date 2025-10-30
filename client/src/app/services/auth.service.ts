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
  private readonly baseUrl = `${environment.apiUrl}`;

  private _authState = signal<LoginResponse | null>(null);
  authState = computed(() => this._authState());

  isLoggedIn = computed(() => !!this._authState()?.accessToken);

  constructor() {
    // Only access localStorage in browser environment
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem('accessToken');
      const refreshToken = localStorage.getItem('refreshToken');
      if (token && refreshToken) {
        this._authState.set({
          tokenType: 'Bearer',
          accessToken: token,
          expiresIn: 0,
          refreshToken: refreshToken
        });
      }
    }
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login?useCookies=false`, payload)
      .pipe(
        tap((res) => {
          this._authState.set(res);
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem('accessToken', res.accessToken);
            localStorage.setItem('refreshToken', res.refreshToken);
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
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<LoginResponse>(`${this.baseUrl}/refresh`, { refreshToken })
      .subscribe({
        next: (res) => this._authState.set(res),
        error: (err) => this.logout()
      });
  }
}
