import { Directive, effect, inject, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { RoleService } from '../services/role.service';

/**
 * Structural directive để hiển thị/ẩn element dựa trên role của user
 * 
 * Sử dụng:
 * <button *hasRole="'Giam Doc'">Chỉ Giám Đốc thấy</button>
 * <div *hasRole="['Giam Doc', 'Pho Giam Doc']">Giám Đốc và Phó Giám Đốc thấy</div>
 */
@Directive({
  selector: '[hasRole]',
  standalone: true
})
export class HasRoleDirective {
  private roleService = inject(RoleService);
  private templateRef = inject(TemplateRef<any>);
  private viewContainer = inject(ViewContainerRef);

  @Input() set hasRole(roles: string | string[]) {
    effect(() => {
      const rolesToCheck = Array.isArray(roles) ? roles : [roles];
      const hasPermission = this.roleService.hasRoles(rolesToCheck);

      if (hasPermission) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      } else {
        this.viewContainer.clear();
      }
    });
  }
}
