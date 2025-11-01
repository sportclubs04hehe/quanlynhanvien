import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard để kiểm tra user có role cụ thể hay không
 * 
 * Sử dụng trong route:
 * {
 *   path: 'admin',
 *   canActivate: [roleGuard],
 *   data: { roles: ['Giam Doc', 'Pho Giam Doc'] }
 * }
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUser();
  
  // Kiểm tra đã login chưa
  if (!currentUser) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  // Lấy required roles từ route data
  const requiredRoles = route.data['roles'] as string[];
  
  // Nếu không có yêu cầu role cụ thể, cho phép truy cập
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  // Kiểm tra user có ít nhất 1 trong các role yêu cầu
  const hasRole = currentUser.roles.some(role => 
    requiredRoles.includes(role)
  );

  if (hasRole) {
    return true;
  }

  // Không có quyền → redirect về trang chủ hoặc 403
  console.error('Access denied: User does not have required role');
  router.navigate(['/dashboard']);
  return false;
};
