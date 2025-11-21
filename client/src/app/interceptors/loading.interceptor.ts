import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { SpinnerService } from '../services/spinner.service';

/**
 * Loading Interceptor - Show spinner chỉ khi API call lâu hơn 300ms
 * 
 * Logic:
 * 1. Khi bắt đầu request, set timeout 300ms
 * 2. Nếu request xong trước 300ms → Cancel timeout, không show spinner
 * 3. Nếu request > 300ms → Show spinner, hide khi xong
 * 
 * UX Benefits:
 * - Fast requests (< 300ms): Không bị flash spinner
 * - Slow requests (> 300ms): User thấy loading indicator
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const spinner = inject(SpinnerService);
  
  let spinnerShown = false;
  let timeoutId: any;

  // Set timeout để show spinner sau 300ms
  timeoutId = setTimeout(() => {
    spinnerShown = true;
    spinner.show('Đang tải dữ liệu...');
  }, 300);

  return next(req).pipe(
    finalize(() => {
      // Clear timeout nếu request xong trước 300ms
      if (timeoutId) {
        clearTimeout(timeoutId);
      }

      // Hide spinner nếu đã show
      if (spinnerShown) {
        spinner.hide();
      }
    })
  );
};
