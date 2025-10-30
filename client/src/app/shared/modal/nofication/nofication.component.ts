import { Component, Input, inject } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-nofication',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './nofication.component.html',
  styleUrl: './nofication.component.css'
})
export class NoficationComponent {
  activeModal = inject(NgbActiveModal);

  @Input() title: string = 'Xác nhận';
  @Input() message: string = 'Bạn có chắc chắn muốn thực hiện hành động này?';
  @Input() confirmText: string = 'Xác nhận';
  @Input() cancelText: string = 'Hủy';
  @Input() type: 'danger' | 'warning' | 'info' | 'success' = 'warning';

  confirm() {
    this.activeModal.close(true);
  }

  cancel() {
    this.activeModal.dismiss(false);
  }
}
