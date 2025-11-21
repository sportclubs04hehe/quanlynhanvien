import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { PhongbanService } from '../../../services/phongban.service';
import { ToastrService } from 'ngx-toastr';
import { PhongBanDto } from '../../../types/phongban.model';
import { BulkQuotaRequestDto, BulkQuotaResultDto } from '../../../types/lichnghi.model';

@Component({
  selector: 'app-lich-admin-quota-bulk',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lich-admin-quota-bulk.component.html',
  styleUrl: './lich-admin-quota-bulk.component.css'
})
export class LichAdminQuotaBulkComponent {
  private fb = inject(FormBuilder);
  private lichNghiService = inject(LichNghiService);
  private phongBanService = inject(PhongbanService);
  private toastr = inject(ToastrService);

  // Outputs
  saved = output<void>();
  cancelled = output<void>();

  // State
  phongBans = signal<PhongBanDto[]>([]);
  isSubmitting = false;

  // Form
  bulkForm: FormGroup;

  // Year/Month options
  readonly years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - 2 + i);
  readonly months = [
    { value: 1, label: 'Tháng 1' },
    { value: 2, label: 'Tháng 2' },
    { value: 3, label: 'Tháng 3' },
    { value: 4, label: 'Tháng 4' },
    { value: 5, label: 'Tháng 5' },
    { value: 6, label: 'Tháng 6' },
    { value: 7, label: 'Tháng 7' },
    { value: 8, label: 'Tháng 8' },
    { value: 9, label: 'Tháng 9' },
    { value: 10, label: 'Tháng 10' },
    { value: 11, label: 'Tháng 11' },
    { value: 12, label: 'Tháng 12' }
  ];

  constructor() {
    const now = new Date();
    
    this.bulkForm = this.fb.group({
      nam: [now.getFullYear(), [Validators.required]],
      thang: [now.getMonth() + 1, [Validators.required, Validators.min(1), Validators.max(12)]],
      soNgayPhepThang: [1, [Validators.required, Validators.min(0), Validators.max(31)]],
      phongBanId: ['all'],
      ghiChu: ['', [Validators.maxLength(500)]]
    });

    this.loadPhongBans();
  }

  loadPhongBans(): void {
    this.phongBanService.getAll(1, 100).subscribe({
      next: (result) => {
        this.phongBans.set(result.items);
      },
      error: (error) => {
        console.error('Error loading phong bans:', error);
      }
    });
  }

  onSubmit(): void {
    if (this.bulkForm.invalid) {
      this.bulkForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;

    const formValue = this.bulkForm.value;
    const request: BulkQuotaRequestDto = {
      nam: formValue.nam,
      thang: formValue.thang,
      soNgayPhepThang: formValue.soNgayPhepThang,
      phongBanId: formValue.phongBanId === 'all' ? undefined : formValue.phongBanId,
      ghiChu: formValue.ghiChu || undefined
    };

    this.lichNghiService.bulkCreateOrUpdateQuota(request).subscribe({
      next: (result: BulkQuotaResultDto) => {
        this.toastr.success(result.message, 'Thành công');
        if (result.errors.length > 0) {
          this.toastr.warning(`Có ${result.errors.length} lỗi xảy ra`, 'Cảnh báo');
        }
        this.isSubmitting = false;
        this.saved.emit();
      },
      error: (err: any) => {
        console.error('Error in bulk create/update:', err);
        this.toastr.error(err.error?.message || 'Không thể cấu hình hàng loạt', 'Lỗi');
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  isInvalid(field: string): boolean {
    const control = this.bulkForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.bulkForm.get(field);
    if (!control || !control.errors) return '';

    if (control.errors['required']) return 'Trường này là bắt buộc';
    if (control.errors['min']) return 'Giá trị không hợp lệ';
    if (control.errors['max']) return 'Giá trị không hợp lệ';
    if (control.errors['maxlength']) return 'Vượt quá độ dài cho phép';

    return 'Giá trị không hợp lệ';
  }
}
