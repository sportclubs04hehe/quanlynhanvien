import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take, Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Interceptor để xử lý lỗi HTTP với auto refresh token
 * - 401: Token hết hạn → Thử refresh token → Retry request
 * - 403: Không có quyền truy cập
 * - 500: Lỗi server
 */

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // KHÔNG auto-refresh cho login endpoint (login fail không phải token hết hạn)
        if (req.url.includes('/login') || req.url.includes('/refresh-token')) {
          console.warn('Authentication failed - Not attempting refresh');
          return throwError(() => error);
        }

        // Thử refresh token cho các API khác
        return handle401Error(req, next, authService, router);
      } else if (error.status === 403) {
        // Không có quyền truy cập
        console.error('403 Forbidden - Bạn không có quyền truy cập tài nguyên này');
      } else if (error.status === 500) {
        // Lỗi server
        console.error('500 Internal Server Error:', error.message);
      }

      return throwError(() => error);
    })
  );
};

function handle401Error(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((response) => {
        isRefreshing = false;
        refreshTokenSubject.next(response.accessToken);

        // Retry original request với token mới
        const clonedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${response.accessToken}`
          }
        });

        return next(clonedReq);
      }),
      catchError((err) => {
        isRefreshing = false;
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
        return throwError(() => err);
      })
    );
  } else {
    // Đang refresh token, đợi cho đến khi có token mới
    return refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap((token) => {
        const clonedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
        return next(clonedReq);
      })
    );
  }
}
