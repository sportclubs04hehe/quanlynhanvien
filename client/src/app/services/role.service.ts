import { computed, inject, Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { APP_ROLES } from '../constants/roles.constants';

/**
 * Service helper để kiểm tra roles và permissions
 */
@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private authService = inject(AuthService);

  // Computed signals cho từng role
  isGiamDoc = computed(() => 
    this.authService.currentUser()?.roles.includes(APP_ROLES.GIAM_DOC) ?? false
  );

  isTruongPhong = computed(() => 
    this.authService.currentUser()?.roles.includes(APP_ROLES.TRUONG_PHONG) ?? false
  );

  isNhanVien = computed(() => 
    this.authService.currentUser()?.roles.includes(APP_ROLES.NHAN_VIEN) ?? false
  );

  // Check nếu user có ít nhất 1 trong các role được chỉ định
  hasAnyRole = computed(() => (roles: string[]) => {
    const userRoles = this.authService.currentUser()?.roles ?? [];
    return roles.some(role => userRoles.includes(role));
  });

  // Check nếu user có tất cả các role được chỉ định
  hasAllRoles = computed(() => (roles: string[]) => {
    const userRoles = this.authService.currentUser()?.roles ?? [];
    return roles.every(role => userRoles.includes(role));
  });

  // Kiểm tra có phải Giám Đốc hoặc Trưởng Phòng (có quyền quản lý)
  isManager = computed(() => 
    this.isGiamDoc() || this.isTruongPhong()
  );

  /**
   * Method helper để dùng trong code (không phải signal)
   */
  hasRole(role: string): boolean {
    return this.authService.currentUser()?.roles.includes(role) ?? false;
  }

  hasRoles(roles: string[]): boolean {
    const userRoles = this.authService.currentUser()?.roles ?? [];
    return roles.some(role => userRoles.includes(role));
  }
}
