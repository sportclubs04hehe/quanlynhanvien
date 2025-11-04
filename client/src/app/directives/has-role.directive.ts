import { Directive, effect, inject, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { RoleService } from '../services/role.service';
import { AuthService } from '../services/auth.service';

/**
 * Structural directive để hiển thị/ẩn element dựa trên role của user
 * 
 * Sử dụng:
 * <button *hasRole="'Giam Doc'">Chỉ Giám Đốc thấy</button>
 * <div *hasRole="['Giam Doc', 'Truong Phong']">Giám Đốc và Trưởng Phòng thấy</div>
 */
@Directive({
  selector: '[hasRole]',
  standalone: true
})
export class HasRoleDirective {
  private roleService = inject(RoleService);
  private authService = inject(AuthService);
  private templateRef = inject(TemplateRef<any>);
  private viewContainer = inject(ViewContainerRef);
  private currentRoles: string[] = [];

  constructor() {
    // Effect phải được khởi tạo trong injection context (constructor)
    effect(() => {
      // Subscribe to currentUser signal changes
      this.authService.currentUser();
      
      // Kiểm tra permissions khi user thay đổi
      this.updateView();
    });
  }

  @Input() set hasRole(roles: string | string[]) {
    this.currentRoles = Array.isArray(roles) ? roles : [roles];
    this.updateView();
  }

  private updateView(): void {
    const hasPermission = this.roleService.hasRoles(this.currentRoles);

    if (hasPermission) {
      // Chỉ tạo view nếu chưa có
      if (this.viewContainer.length === 0) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      }
    } else {
      this.viewContainer.clear();
    }
  }
}
