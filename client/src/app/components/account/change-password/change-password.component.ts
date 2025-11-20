import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';
import { ChangePasswordDto } from '../../../types/users.model';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css'
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  passwordForm: FormGroup;
  isSubmitting = signal(false);
  showCurrentPassword = signal(false);
  showNewPassword = signal(false);
  showConfirmPassword = signal(false);

  constructor() {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    
    if (newPassword !== confirmPassword) {
      form.get('confirmPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    
    return null;
  }

  onSubmit() {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    const payload: ChangePasswordDto = this.passwordForm.value;

    this.authService.changePassword(payload).subscribe({
      next: (res) => {
        this.toastr.success(res.message || 'Đổi mật khẩu thành công! Vui lòng đăng nhập lại.', 'Thành công');
        this.passwordForm.reset();
        this.isSubmitting.set(false);
        
        // Logout và redirect sau 2 giây
        setTimeout(() => {
          this.authService.logout();
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        
        if (err.error?.message) {
          this.toastr.error(err.error.message, 'Lỗi');
        } else if (err.error?.errors) {
          // ModelState errors
          const errors = Object.values(err.error.errors).flat().join(', ');
          this.toastr.error(errors as string, 'Lỗi');
        } else {
          this.toastr.error('Đã xảy ra lỗi khi đổi mật khẩu', 'Lỗi');
        }
      }
    });
  }

  togglePasswordVisibility(field: 'current' | 'new' | 'confirm') {
    if (field === 'current') {
      this.showCurrentPassword.update(v => !v);
    } else if (field === 'new') {
      this.showNewPassword.update(v => !v);
    } else {
      this.showConfirmPassword.update(v => !v);
    }
  }

  hasError(controlName: string, errorType: string): boolean {
    const control = this.passwordForm.get(controlName);
    return !!(control?.hasError(errorType) && (control?.dirty || control?.touched));
  }
}
