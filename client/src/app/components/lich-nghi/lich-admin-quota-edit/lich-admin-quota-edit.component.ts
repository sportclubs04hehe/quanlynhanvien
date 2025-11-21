import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { ToastrService } from 'ngx-toastr';
import { NghiPhepQuotaDto, UpsertNghiPhepQuotaDto } from '../../../types/lichnghi.model';

@Component({
  selector: 'app-lich-admin-quota-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lich-admin-quota-edit.component.html',
  styleUrl: './lich-admin-quota-edit.component.css'
})
export class LichAdminQuotaEditComponent {
  private fb = inject(FormBuilder);
  private lichNghiService = inject(LichNghiService);
  private toastr = inject(ToastrService);

  // Input: Quota cần edit
  quota = input.required<NghiPhepQuotaDto>();

  // Output: Sau khi save thành công
  saved = output<void>();
  cancelled = output<void>();

  editForm: FormGroup;
  isSubmitting = false;

  constructor() {
    this.editForm = this.fb.group({
      soNgayPhepThang: [1, [Validators.required, Validators.min(0), Validators.max(31)]],
      ghiChu: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    // Populate form với data từ quota
    const currentQuota = this.quota();
    this.editForm.patchValue({
      soNgayPhepThang: currentQuota.soNgayPhepThang,
      ghiChu: currentQuota.ghiChu || ''
    });
  }

  onSubmit(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const currentQuota = this.quota();

    const dto: UpsertNghiPhepQuotaDto = {
      nhanVienId: currentQuota.nhanVienId,
      nam: currentQuota.nam,
      thang: currentQuota.thang,
      soNgayPhepThang: this.editForm.value.soNgayPhepThang,
      ghiChu: this.editForm.value.ghiChu || null
    };

    this.lichNghiService.updateQuota(currentQuota.id, dto).subscribe({
      next: () => {
        this.toastr.success('Cập nhật hạn mức nghỉ phép thành công');
        this.isSubmitting = false;
        this.saved.emit();
      },
      error: (err) => {
        console.error('Error updating quota:', err);
        this.toastr.error(err.error?.message || 'Không thể cập nhật hạn mức nghỉ phép');
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  // Validation helpers
  isInvalid(field: string): boolean {
    const control = this.editForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.editForm.get(field);
    if (!control || !control.errors) return '';

    if (control.errors['required']) return 'Trường này là bắt buộc';
    if (control.errors['min']) return 'Giá trị phải lớn hơn hoặc bằng 0';
    if (control.errors['max']) return 'Giá trị không được vượt quá 31 ngày';
    if (control.errors['maxlength']) return 'Ghi chú không được vượt quá 500 ký tự';

    return 'Giá trị không hợp lệ';
  }
}
