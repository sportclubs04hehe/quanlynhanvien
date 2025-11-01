import { Component, Input, inject } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-header border-0">
      <h5 class="modal-title">
        <i class="bi bi-exclamation-triangle-fill text-warning me-2"></i>
        {{ title }}
      </h5>
      <button type="button" class="btn-close" aria-label="Close" (click)="cancel()"></button>
    </div>
    <div class="modal-body">
      <p class="mb-0">{{ message }}</p>
    </div>
    <div class="modal-footer border-0">
      <button type="button" class="btn btn-secondary" (click)="cancel()">
        {{ cancelText }}
      </button>
      <button type="button" class="btn btn-warning" (click)="confirm()">
        <i class="bi bi-box-arrow-right me-1"></i>
        {{ confirmText }}
      </button>
    </div>
  `,
  styles: [`
    .modal-header {
      background-color: #fff3cd;
    }
    
    .modal-title {
      color: #856404;
      font-weight: 600;
    }
    
    .modal-body p {
      color: #666;
      font-size: 0.95rem;
    }
    
    .btn-warning {
      background-color: #ffc107;
      border-color: #ffc107;
      color: #000;
    }
    
    .btn-warning:hover {
      background-color: #e0a800;
      border-color: #d39e00;
    }
  `]
})
export class ConfirmDialogComponent {
  activeModal = inject(NgbActiveModal);

  @Input() title: string = 'Xác nhận thoát';
  @Input() message: string = 'Bạn có thay đổi chưa được lưu. Bạn có chắc chắn muốn thoát?';
  @Input() confirmText: string = 'Thoát';
  @Input() cancelText: string = 'Ở lại';

  confirm() {
    this.activeModal.close(true);
  }

  cancel() {
    this.activeModal.dismiss(false);
  }
}
