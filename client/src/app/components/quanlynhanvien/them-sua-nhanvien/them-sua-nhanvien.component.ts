import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { finalize } from 'rxjs';
import { QuanlynhanvienService } from '../../../services/quanlynhanvien.service';
import { PhongbanService } from '../../../services/phongban.service';
import { SpinnerService } from '../../../services/spinner.service';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';
import { RegisterUserDto, UpdateUserDto, UserDto, NhanVienStatus } from '../../../types/users.model';
import { PhongBanDto } from '../../../types/phongban.model';
import { ChucVuDto } from '../../../types/chucvu.model';
import { PagedResult } from '../../../types/page-result.model';
import { ChucvuService } from '../../../services/chucvu.service';
import { CanComponentDeactivate } from '../../../guards/unsaved-changes.guard';

@Component({
  selector: 'app-them-sua-nhanvien',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './them-sua-nhanvien.component.html',
  styleUrl: './them-sua-nhanvien.component.css'
})
export class ThemSuaNhanvienComponent implements OnInit, CanComponentDeactivate {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() userId?: string;
  @Input() userData?: UserDto;

  private fb = inject(FormBuilder);
  private nhanVienService = inject(QuanlynhanvienService);
  private phongBanService = inject(PhongbanService);
  private chucVuService: ChucvuService = inject(ChucvuService);
  private spinner = inject(SpinnerService);
  private modal = inject(NgbModal);
  
  activeModal = inject(NgbActiveModal);

  userForm!: FormGroup;
  errorMessage = signal<string | null>(null);
  showPassword = signal<boolean>(false);
  
  phongBans = signal<PhongBanDto[]>([]);
  chucVus = signal<ChucVuDto[]>([]);
  quanLys = signal<UserDto[]>([]);
  
  // Status enum options
  statusOptions = [
    { value: NhanVienStatus.Active, label: 'Hoạt động' },
    { value: NhanVienStatus.Inactive, label: 'Ngừng hoạt động' },
    { value: NhanVienStatus.OnLeave, label: 'Nghỉ phép' }
  ];

  private isDirty = false;

  ngOnInit() {
    this.initForm();
    this.loadDropdownData();
    
    if (this.mode === 'edit' && this.userId) {
      this.loadUserData();
    }

    this.userForm.valueChanges.subscribe(() => {
      this.isDirty = true;
    });
  }

  // CanComponentDeactivate interface implementation
  canDeactivate(): boolean {
    return !this.isDirty;
  }

  private initForm() {
    if (this.mode === 'create') {
      this.userForm = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['123456'], // Mật khẩu mặc định
        tenDayDu: ['', [Validators.required]],
        phoneNumber: [''],
        phongBanId: [''],
        chucVuId: [''],
        quanLyId: [''],
        ngaySinh: [''],
        ngayVaoLam: [''],
        telegramChatId: ['']
      });
    } else {
      this.userForm = this.fb.group({
        tenDayDu: ['', [Validators.required]],
        phoneNumber: [''],
        phongBanId: [''],
        chucVuId: [''],
        quanLyId: [''],
        ngaySinh: [''],
        ngayVaoLam: [''],
        telegramChatId: [''],
        status: ['']
      });
    }
  }

  private loadUserData() {
    if (!this.userId) return;

    this.spinner.show('Đang tải thông tin nhân viên...');
    this.nhanVienService.getById(this.userId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (user) => {
          this.userForm.patchValue({
            tenDayDu: user.tenDayDu,
            phoneNumber: user.phoneNumber,
            phongBanId: user.phongBan?.id || '',
            chucVuId: user.chucVu?.id || '',
            ngaySinh: user.ngaySinh ? this.formatDateForInput(user.ngaySinh) : '',
            ngayVaoLam: user.ngayVaoLam ? this.formatDateForInput(user.ngayVaoLam) : '',
            telegramChatId: user.telegramChatId,
            status: user.status ?? ''  // Sử dụng nullish coalescing để giữ giá trị 0
          });
          this.isDirty = false;
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải thông tin nhân viên');
          console.error('Error loading user:', error);
        }
      });
  }

  private loadDropdownData() {
    this.phongBanService.getAll(1, 100).subscribe({
      next: (result: PagedResult<PhongBanDto>) => this.phongBans.set(result.items),
      error: (err: any) => console.error('Error loading phong bans:', err)
    });

    this.chucVuService.getAll(1, 100).subscribe({
      next: (result: PagedResult<ChucVuDto>) => this.chucVus.set(result.items),
      error: (err: any) => console.error('Error loading chuc vus:', err)
    });

    this.nhanVienService.getAll(1, 1000).subscribe({
      next: (result: PagedResult<UserDto>) => this.quanLys.set(result.items),
      error: (err: any) => console.error('Error loading quan lys:', err)
    });
  }

  onSubmit() {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    const formValue = this.userForm.value;
    
    if (this.mode === 'create') {
      const dto: RegisterUserDto = {
        email: formValue.email,
        password: formValue.password || '123456', // Sử dụng mật khẩu mặc định nếu rỗng
        tenDayDu: formValue.tenDayDu,
        phoneNumber: formValue.phoneNumber || undefined,
        phongBanId: formValue.phongBanId || undefined,
        chucVuId: formValue.chucVuId || undefined,
        quanLyId: formValue.quanLyId || undefined,
        ngaySinh: formValue.ngaySinh ? this.parseDateAsUTC(formValue.ngaySinh) : undefined,
        ngayVaoLam: formValue.ngayVaoLam ? this.parseDateAsUTC(formValue.ngayVaoLam) : undefined,
        telegramChatId: formValue.telegramChatId || undefined
      };

      this.spinner.show('Đang tạo nhân viên...');
      this.nhanVienService.register(dto)
        .pipe(finalize(() => this.spinner.hide()))
        .subscribe({
          next: () => {
            this.isDirty = false;
            this.activeModal.close(true);
          },
          error: (error) => {
            this.errorMessage.set(error.error?.message || 'Không thể tạo nhân viên');
            console.error('Error creating user:', error);
          }
        });
    } else {
      const dto: UpdateUserDto = {
        tenDayDu: formValue.tenDayDu,
        phoneNumber: formValue.phoneNumber || undefined,
        phongBanId: formValue.phongBanId || undefined,
        chucVuId: formValue.chucVuId || undefined,
        quanLyId: formValue.quanLyId || undefined,
        ngaySinh: formValue.ngaySinh ? this.parseDateAsUTC(formValue.ngaySinh) : undefined,
        ngayVaoLam: formValue.ngayVaoLam ? this.parseDateAsUTC(formValue.ngayVaoLam) : undefined,
        telegramChatId: formValue.telegramChatId || undefined,
        status: formValue.status !== '' && formValue.status !== null && formValue.status !== undefined 
          ? formValue.status 
          : undefined
      };

      this.spinner.show('Đang cập nhật nhân viên...');
      this.nhanVienService.update(this.userId!, dto)
        .pipe(finalize(() => this.spinner.hide()))
        .subscribe({
          next: () => {
            this.isDirty = false;
            this.activeModal.close(true);
          },
          error: (error) => {
            this.errorMessage.set(error.error?.message || 'Không thể cập nhật nhân viên');
            console.error('Error updating user:', error);
          }
        });
    }
  }

  dismiss() {
    if (this.isDirty) {
      const modalRef = this.modal.open(ConfirmDialogComponent, {
        centered: true,
        backdrop: 'static'
      });

      modalRef.result.then(
        (confirmed) => {
          if (confirmed) {
            this.activeModal.dismiss();
          }
        },
        () => {}
      );
    } else {
      this.activeModal.dismiss();
    }
  }

  isFieldInvalid(field: string): boolean {
    const control = this.userForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.userForm.get(field);
    if (!control) return '';

    if (control.hasError('required')) {
      return 'Trường này là bắt buộc';
    }
    if (control.hasError('email')) {
      return 'Email không hợp lệ';
    }
    if (control.hasError('minlength')) {
      const minLength = control.getError('minlength').requiredLength;
      return `Tối thiểu ${minLength} ký tự`;
    }
    return '';
  }

  get isCreateMode(): boolean {
    return this.mode === 'create';
  }

  get title(): string {
    return this.mode === 'create' ? 'Thêm Nhân Viên Mới' : 'Chỉnh Sửa Nhân Viên';
  }

  private formatDateForInput(dateValue: Date | string): string {
    if (!dateValue) return '';
    
    // Nếu là string, parse theo UTC để tránh timezone shift
    const date = typeof dateValue === 'string' 
      ? new Date(dateValue + 'Z')  // Thêm 'Z' để parse theo UTC
      : dateValue;
    
    // Lấy năm-tháng-ngày theo UTC
    const year = date.getUTCFullYear();
    const month = String(date.getUTCMonth() + 1).padStart(2, '0');
    const day = String(date.getUTCDate()).padStart(2, '0');
    
    return `${year}-${month}-${day}`;
  }

  private parseDateAsUTC(dateString: string): Date {
    if (!dateString) return new Date();
    
    // Split YYYY-MM-DD
    const [year, month, day] = dateString.split('-').map(Number);
    
    // Tạo Date theo UTC (tránh timezone local)
    return new Date(Date.UTC(year, month - 1, day));
  }

  togglePasswordVisibility() {
    this.showPassword.set(!this.showPassword());
  }
}
