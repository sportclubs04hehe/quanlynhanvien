import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { PhongbanService } from '../../../services/phongban.service';
import { CommonModule } from '@angular/common';
import { PhongBanDto } from '../../../types/phongban.model';
import { CanComponentDeactivate } from '../../../guards/unsaved-changes.guard';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-them-sua-phongban',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './them-sua-phongban.component.html',
  styleUrl: './them-sua-phongban.component.css'
})
export class ThemSuaPhongbanComponent implements OnInit, CanComponentDeactivate {
  modal = inject(NgbActiveModal);
  private fb = inject(FormBuilder);
  private phongbanService = inject(PhongbanService);
  private ngbModal = inject(NgbModal);

  @Input() mode: 'create' | 'edit' = 'create';
  @Input() phongBanId?: string;
  @Input() phongBanData?: PhongBanDto;

  phongBanForm!: FormGroup;
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  private isDirty = false;

  ngOnInit() {
    this.initForm();
    
    if (this.mode === 'edit' && this.phongBanData) {
      this.phongBanForm.patchValue({
        tenPhongBan: this.phongBanData.tenPhongBan,
        moTa: this.phongBanData.moTa
      });
    }

    // Track form changes
    this.phongBanForm.valueChanges.subscribe(() => {
      this.isDirty = true;
    });
  }

  // CanComponentDeactivate interface implementation
  canDeactivate(): boolean {
    return !this.isDirty;
  }

  initForm() {
    this.phongBanForm = this.fb.group({
      tenPhongBan: ['', [Validators.required, Validators.maxLength(100)]],
      moTa: ['', [Validators.maxLength(500)]]
    });
  }

  get tenPhongBan() {
    return this.phongBanForm.get('tenPhongBan');
  }

  get moTa() {
    return this.phongBanForm.get('moTa');
  }

  get title(): string {
    return this.mode === 'create' ? 'Thêm Phòng Ban Mới' : 'Chỉnh Sửa Phòng Ban';
  }

  get isCreateMode(): boolean {
    return this.mode === 'create';
  }

  onSubmit() {
    if (this.phongBanForm.invalid) {
      Object.keys(this.phongBanForm.controls).forEach(key => {
        this.phongBanForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const formData = this.phongBanForm.value;

    if (this.mode === 'create') {
      this.phongbanService.create(formData).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.isDirty = false; // Reset dirty flag after successful save
          this.modal.close(true);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(error.error?.message || 'Không thể tạo phòng ban. Vui lòng thử lại.');
          console.error('Error creating phong ban:', error);
        }
      });
    } else if (this.mode === 'edit' && this.phongBanId) {
      this.phongbanService.update(this.phongBanId, formData).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.isDirty = false; // Reset dirty flag after successful save
          this.modal.close(true);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(error.error?.message || 'Không thể cập nhật phòng ban. Vui lòng thử lại.');
          console.error('Error updating phong ban:', error);
        }
      });
    }
  }

  onCancel() {
    if (this.isDirty) {
      const modalRef = this.ngbModal.open(ConfirmDialogComponent, {
        centered: true,
        backdrop: 'static'
      });

      modalRef.result.then(
        (confirmed) => {
          if (confirmed) {
            this.modal.dismiss();
          }
        },
        () => {} // Dismissed - do nothing
      );
    } else {
      this.modal.dismiss();
    }
  }
}

