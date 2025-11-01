import { CanDeactivateFn } from '@angular/router';

export interface CanComponentDeactivate {
  canDeactivate(): boolean;
}

/**
 * Guard để kiểm tra các thay đổi chưa lưu trước khi rời khỏi route.
 * Component có thể implement interface CanComponentDeactivate để tùy chỉnh logic.
 * 
 * Lưu ý: Guard này chỉ hoạt động khi rời khỏi route (chuyển trang),
 * không hoạt động với modal vì modal không thay đổi route.
 * Để xử lý đóng modal, sử dụng ConfirmDialogComponent trong modal component.
 */
export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (
  component: CanComponentDeactivate
) => {
  // Nếu component không implement canDeactivate, cho phép rời khỏi
  if (!component.canDeactivate) {
    return true;
  }

  // Nếu không có thay đổi, cho phép rời khỏi
  if (component.canDeactivate()) {
    return true;
  }

  // Có thay đổi chưa lưu - hiển thị confirm dialog
  return confirm('Bạn có thay đổi chưa được lưu. Bạn có chắc chắn muốn rời khỏi trang?');
};
