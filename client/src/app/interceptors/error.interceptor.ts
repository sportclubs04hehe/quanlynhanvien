import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Interceptor để xử lý lỗi HTTP
 * - 401: Token hết hạn hoặc không hợp lệ → Logout và redirect về login
 * - 403: Không có quyền truy cập
 * - 500: Lỗi server
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Token hết hạn hoặc không hợp lệ
        console.warn('401 Unauthorized - Token hết hạn hoặc không hợp lệ');
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
      } else if (error.status === 403) {
        // Không có quyền truy cập
        console.error('403 Forbidden - Bạn không có quyền truy cập tài nguyên này');
        // Có thể hiển thị toast notification ở đây
      } else if (error.status === 500) {
        // Lỗi server
        console.error('500 Internal Server Error:', error.message);
      }

      return throwError(() => error);
    })
  );
};
