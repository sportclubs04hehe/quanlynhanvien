import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { finalize } from 'rxjs';
import { QuanlynhanvienService } from '../../../services/quanlynhanvien.service';
import { SpinnerService } from '../../../services/spinner.service';
import { UserDto, NhanVienStatus } from '../../../types/users.model';

@Component({
  selector: 'app-chitiet-nhanvien',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chitiet-nhanvien.component.html',
  styleUrl: './chitiet-nhanvien.component.css'
})
export class ChitietNhanvienComponent implements OnInit {
  @Input() userId!: string;

  private nhanVienService = inject(QuanlynhanvienService);
  private spinner = inject(SpinnerService);
  activeModal = inject(NgbActiveModal);

  user = signal<UserDto | null>(null);
  errorMessage = signal<string | null>(null);

  // Enum reference for template
  NhanVienStatus = NhanVienStatus;

  ngOnInit() {
    this.loadUserDetail();
  }

  private loadUserDetail() {
    if (!this.userId) {
      this.errorMessage.set('Không tìm thấy thông tin nhân viên');
      return;
    }

    this.spinner.show('Đang tải thông tin chi tiết...');
    this.nhanVienService.getById(this.userId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (user) => {
          this.user.set(user);
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải thông tin nhân viên');
          console.error('Error loading user detail:', error);
        }
      });
  }

  close() {
    this.activeModal.dismiss();
  }

  getStatusLabel(status: NhanVienStatus): string {
    switch (status) {
      case NhanVienStatus.Active:
        return 'Hoạt động';
      case NhanVienStatus.Inactive:
        return 'Ngừng hoạt động';
      case NhanVienStatus.OnLeave:
        return 'Nghỉ phép';
      default:
        return 'Không xác định';
    }
  }

  getStatusClass(status: NhanVienStatus): string {
    switch (status) {
      case NhanVienStatus.Active:
        return 'bg-success';
      case NhanVienStatus.Inactive:
        return 'bg-danger';
      case NhanVienStatus.OnLeave:
        return 'bg-warning';
      default:
        return 'bg-secondary';
    }
  }
}
