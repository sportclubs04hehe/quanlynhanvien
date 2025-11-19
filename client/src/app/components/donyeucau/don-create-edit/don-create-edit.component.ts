import { Component, inject, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { NgbActiveModal, NgbCalendar, NgbDate, NgbDatepickerModule, NgbDateParserFormatter, NgbDateStruct, NgbTimepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { SpinnerService } from '../../../services/spinner.service';
import { 
  CreateDonYeuCauDto, 
  UpdateDonYeuCauDto, 
  DonYeuCauDto, 
  LoaiDonYeuCau,
  LoaiNghiPhep,
  NgayNghiInfo,
  LOAI_DON_DISPLAY_NAMES,
  LOAI_NGHI_PHEP_DISPLAY_NAMES
} from '../../../types/don.model';
import { CanComponentDeactivate } from '../../../guards/unsaved-changes.guard';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-don-create-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbDatepickerModule, NgbTimepickerModule],
  templateUrl: './don-create-edit.component.html',
  styleUrl: './don-create-edit.component.css'
})
export class DonCreateEditComponent implements OnInit, CanComponentDeactivate {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() donId?: string;
  @Input() donData?: DonYeuCauDto;
  
  private fb = inject(FormBuilder);
  private donService = inject(DonYeuCauService);
  private spinner = inject(SpinnerService);
  private modal = inject(NgbModal);
  private toastr = inject(ToastrService);
  activeModal = inject(NgbActiveModal);
  calendar = inject(NgbCalendar);
  formatter = inject(NgbDateParserFormatter);
  
  donForm!: FormGroup;
  private isDirty = false;
  
  // Date picker properties
  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = null;
  toDate: NgbDate | null = null;
  ngayNghi: NgbDate | null = null; // Single date for half-day/one-day leave
  minDate!: NgbDate; // Minimum date = today (không chọn quá khứ)
  requestedLeaveDates: Map<string, NgayNghiInfo> = new Map(); // Ngày đã nghỉ với thông tin chi tiết
  
  // Expose enum to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly LoaiNghiPhep = LoaiNghiPhep;
  readonly loaiDonOptions = Object.entries(LOAI_DON_DISPLAY_NAMES).map(([key, value]) => ({
    value: key as LoaiDonYeuCau,
    label: value
  }));
  readonly loaiNghiPhepOptions = Object.entries(LOAI_NGHI_PHEP_DISPLAY_NAMES).map(([key, value]) => ({
    value: key as LoaiNghiPhep,
    label: value
  }));
  
  ngOnInit(): void {
    // Set minDate to today
    const today = new Date();
    this.minDate = new NgbDate(today.getFullYear(), today.getMonth() + 1, today.getDate());
    
    // Load ngày đã nghỉ cho cả create và edit mode
    this.loadRequestedLeaveDates();
    
    this.initForm();
    
    if (this.mode === 'edit' && this.donId) {
      this.loadDonData();
    }
    
    // Track form changes
    this.donForm.valueChanges.subscribe(() => {
      this.isDirty = true;
    });
    
    // Watch loaiDon changes to update validators
    this.donForm.get('loaiDon')?.valueChanges.subscribe((loaiDon: LoaiDonYeuCau) => {
      this.updateValidators(loaiDon);
    });
    
    // Watch loaiNghiPhep changes to reset date fields
    this.donForm.get('loaiNghiPhep')?.valueChanges.subscribe((loaiNghiPhep: LoaiNghiPhep) => {
      this.onLoaiNghiPhepChange(loaiNghiPhep);
    });
  }
  
  /**
   * CanComponentDeactivate implementation
   */
  canDeactivate(): boolean {
    return !this.isDirty;
  }
  
  /**
   * Load danh sách ngày đã nghỉ để highlight
   * Chỉ load 3 tháng tiếp theo để tối ưu performance
   */
  private loadRequestedLeaveDates(): void {
    const fromDate = new Date();
    const toDate = new Date();
    toDate.setMonth(toDate.getMonth() + 3); // Chỉ load 3 tháng tới
    
    this.donService.getNgayDaNghi(fromDate, toDate)
      .subscribe({
        next: (ngayNghiInfos) => {
          // Convert to Map for fast lookup
          this.requestedLeaveDates = new Map(
            ngayNghiInfos.map(info => {
              const date = new Date(info.ngay);
              const dateKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
              return [dateKey, info];
            })
          );
        },
        error: (error) => {
          console.error('Error loading requested leave dates:', error);
        }
      });
  }
  
  /**
   * Initialize form based on mode
   */
  private initForm(): void {
    this.donForm = this.fb.group({
      loaiDon: [
        { value: this.mode === 'create' ? LoaiDonYeuCau.NghiPhep : null, disabled: this.mode === 'edit' },
        Validators.required
      ],
      lyDo: ['', [Validators.required, Validators.maxLength(500)]],
      
      // Nghỉ Phép - Loại nghỉ chi tiết
      loaiNghiPhep: [this.mode === 'create' ? LoaiNghiPhep.MotNgay : null],
      
      // Nghỉ Phép & Công Tác
      ngayBatDau: [null],
      ngayKetThuc: [null],
      
      // Làm Thêm Giờ
      ngayLamThem: [null],
      soGioLamThem: [null, [Validators.min(0.5), Validators.max(24)]],
      
      // Đi Muộn
      ngayDiMuon: [null],
      gioDuKienDen: [{ hour: 9, minute: 0 }],
      
      // Công Tác
      diaDiemCongTac: ['', Validators.maxLength(200)],
      mucDichCongTac: ['', Validators.maxLength(500)]
    });
    
    // Set initial validators for default loaiDon
    if (this.mode === 'create') {
      this.updateValidators(LoaiDonYeuCau.NghiPhep);
    }
  }
  
  /**
   * Update validators based on LoaiDon
   */
  private updateValidators(loaiDon: LoaiDonYeuCau): void {
    // Clear all conditional validators first
    this.donForm.get('loaiNghiPhep')?.clearValidators();
    this.donForm.get('ngayBatDau')?.clearValidators();
    this.donForm.get('ngayKetThuc')?.clearValidators();
    this.donForm.get('ngayLamThem')?.clearValidators();
    this.donForm.get('soGioLamThem')?.clearValidators();
    this.donForm.get('ngayDiMuon')?.clearValidators();
    this.donForm.get('gioDuKienDen')?.clearValidators();
    this.donForm.get('diaDiemCongTac')?.clearValidators();
    this.donForm.get('mucDichCongTac')?.clearValidators();
    
    // Apply validators based on loaiDon
    switch (loaiDon) {
      case LoaiDonYeuCau.NghiPhep:
        this.donForm.get('loaiNghiPhep')?.setValidators(Validators.required);
        this.donForm.get('ngayBatDau')?.setValidators(Validators.required);
        this.donForm.get('ngayKetThuc')?.setValidators(Validators.required);
        break;
        
      case LoaiDonYeuCau.LamThemGio:
        this.donForm.get('ngayLamThem')?.setValidators(Validators.required);
        this.donForm.get('soGioLamThem')?.setValidators([Validators.required, Validators.min(0.5), Validators.max(24)]);
        break;
        
      case LoaiDonYeuCau.DiMuon:
        this.donForm.get('ngayDiMuon')?.setValidators(Validators.required);
        this.donForm.get('gioDuKienDen')?.setValidators(Validators.required);
        break;
        
      case LoaiDonYeuCau.CongTac:
        this.donForm.get('ngayBatDau')?.setValidators(Validators.required);
        this.donForm.get('ngayKetThuc')?.setValidators(Validators.required);
        this.donForm.get('diaDiemCongTac')?.setValidators([Validators.required, Validators.maxLength(200)]);
        this.donForm.get('mucDichCongTac')?.setValidators([Validators.required, Validators.maxLength(500)]);
        break;
    }
    
    // Update validity
    Object.keys(this.donForm.controls).forEach(key => {
      this.donForm.get(key)?.updateValueAndValidity({ emitEvent: false });
    });
  }
  
  /**
   * Load don data for edit mode
   */
  private loadDonData(): void {
    if (!this.donId) return;
    
    this.spinner.show('Đang tải thông tin đơn...');
    this.donService.getById(this.donId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (don) => {
          this.patchFormWithDonData(don);
          this.isDirty = false;
        },
        error: (error) => {
          this.toastr.error('Không thể tải thông tin đơn', 'Thông báo');
          console.error('Error loading don:', error);
        }
      });
  }
  
  /**
   * Patch form with don data
   */
  private patchFormWithDonData(don: DonYeuCauDto): void {
    const ngayBatDau = don.ngayBatDau ? this.dateToNgbDateStruct(don.ngayBatDau) : null;
    const ngayKetThuc = don.ngayKetThuc ? this.dateToNgbDateStruct(don.ngayKetThuc) : null;
    const ngayLamThem = don.ngayLamThem ? this.dateToNgbDateStruct(don.ngayLamThem) : null;
    const ngayDiMuon = don.ngayDiMuon ? this.dateToNgbDateStruct(don.ngayDiMuon) : null;
    const gioDuKienDen = don.gioDuKienDen ? this.dateToTimeStruct(don.gioDuKienDen) : { hour: 9, minute: 0 };
    
    // Set dates for datepicker based on loaiNghiPhep
    if (ngayBatDau && ngayKetThuc) {
      const startDate = NgbDate.from(ngayBatDau);
      const endDate = NgbDate.from(ngayKetThuc);
      
      // Check if it's single date (same start and end date)
      if (startDate && endDate && startDate.equals(endDate)) {
        this.ngayNghi = startDate;
        this.fromDate = null;
        this.toDate = null;
      } else if (startDate && endDate) {
        // Range date
        this.fromDate = startDate;
        this.toDate = endDate;
        this.ngayNghi = null;
      }
    }
    
    this.donForm.patchValue({
      loaiDon: don.loaiDon,
      lyDo: don.lyDo,
      loaiNghiPhep: don.loaiNghiPhep || null,
      ngayBatDau,
      ngayKetThuc,
      ngayLamThem,
      soGioLamThem: don.soGioLamThem,
      ngayDiMuon,
      gioDuKienDen,
      diaDiemCongTac: don.diaDiemCongTac || '',
      mucDichCongTac: don.mucDichCongTac || ''
    });
    
    // Update validators for loaded loaiDon
    this.updateValidators(don.loaiDon);
  }
  
  /**
   * Submit form
   */
  onSubmit(): void {
    if (this.donForm.invalid) {
      this.markFormGroupTouched(this.donForm);
      this.toastr.warning('Vui lòng điền đầy đủ thông tin bắt buộc', 'Cảnh báo');
      return;
    }
    
    if (this.mode === 'create') {
      this.createDon();
    } else {
      this.updateDon();
    }
  }
  
  /**
   * Create new don
   */
  private createDon(): void {
    const dto = this.buildCreateDto();
    
    this.spinner.show('Đang tạo đơn...');
    this.donService.create(dto)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (result) => {
          this.isDirty = false;
          // Blur focused element before closing modal
          if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
          }
          // Use setTimeout to ensure blur completes
          setTimeout(() => {
            this.activeModal.close(result);
          }, 0);
        },
        error: (error) => {
          // Extract error message from backend
          const message = error.error?.message || 'Không thể tạo đơn. Vui lòng thử lại.';
          this.toastr.error(message, 'Thông báo');
          console.error('Error creating don:', error);
        }
      });
  }
  
  /**
   * Update existing don
   */
  private updateDon(): void {
    if (!this.donId) return;
    
    const dto = this.buildUpdateDto();
    
    this.spinner.show('Đang cập nhật đơn...');
    this.donService.update(this.donId, dto)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (result) => {
          this.isDirty = false;
          // Blur focused element before closing modal
          if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
          }
          // Use setTimeout to ensure blur completes
          setTimeout(() => {
            this.activeModal.close(result);
          }, 0);
        },
        error: (error) => {
          // Extract error message from backend
          const message = error.error?.message || 'Không thể cập nhật đơn. Vui lòng thử lại.';
          this.toastr.error(message, 'Thông báo');
          console.error('Error updating don:', error);
        }
      });
  }
  
  /**
   * Build CreateDonYeuCauDto from form
   */
  private buildCreateDto(): CreateDonYeuCauDto {
    const formValue = this.donForm.value;
    const loaiDon: LoaiDonYeuCau = formValue.loaiDon;
    
    const dto: CreateDonYeuCauDto = {
      loaiDon,
      lyDo: formValue.lyDo.trim()
    };
    
    // Add loaiNghiPhep if loaiDon is NghiPhep
    if (loaiDon === LoaiDonYeuCau.NghiPhep) {
      dto.loaiNghiPhep = formValue.loaiNghiPhep;
    }
    
    // Add conditional fields based on loaiDon
    if (loaiDon === LoaiDonYeuCau.NghiPhep || loaiDon === LoaiDonYeuCau.CongTac) {
      dto.ngayBatDau = this.ngbDateStructToISOString(formValue.ngayBatDau);
      dto.ngayKetThuc = this.ngbDateStructToISOString(formValue.ngayKetThuc);
    }
    
    if (loaiDon === LoaiDonYeuCau.LamThemGio) {
      dto.ngayLamThem = this.ngbDateStructToISOString(formValue.ngayLamThem);
      dto.soGioLamThem = formValue.soGioLamThem;
    }
    
    if (loaiDon === LoaiDonYeuCau.DiMuon) {
      dto.ngayDiMuon = this.ngbDateStructToISOString(formValue.ngayDiMuon);
      dto.gioDuKienDen = this.combineDateTime(formValue.ngayDiMuon, formValue.gioDuKienDen);
    }
    
    if (loaiDon === LoaiDonYeuCau.CongTac) {
      dto.diaDiemCongTac = formValue.diaDiemCongTac?.trim();
      dto.mucDichCongTac = formValue.mucDichCongTac?.trim();
    }
    
    return dto;
  }
  
  /**
   * Build UpdateDonYeuCauDto from form
   */
  private buildUpdateDto(): UpdateDonYeuCauDto {
    const formValue = this.donForm.getRawValue(); // Use getRawValue() to include disabled fields
    const loaiDon: LoaiDonYeuCau = formValue.loaiDon;
    
    const dto: UpdateDonYeuCauDto = {
      lyDo: formValue.lyDo.trim()
    };
    
    // Add loaiNghiPhep if loaiDon is NghiPhep
    if (loaiDon === LoaiDonYeuCau.NghiPhep) {
      dto.loaiNghiPhep = formValue.loaiNghiPhep;
    }
    
    // Add conditional fields (same logic as create)
    if (loaiDon === LoaiDonYeuCau.NghiPhep || loaiDon === LoaiDonYeuCau.CongTac) {
      dto.ngayBatDau = this.ngbDateStructToISOString(formValue.ngayBatDau);
      dto.ngayKetThuc = this.ngbDateStructToISOString(formValue.ngayKetThuc);
    }
    
    if (loaiDon === LoaiDonYeuCau.LamThemGio) {
      dto.ngayLamThem = this.ngbDateStructToISOString(formValue.ngayLamThem);
      dto.soGioLamThem = formValue.soGioLamThem;
    }
    
    if (loaiDon === LoaiDonYeuCau.DiMuon) {
      dto.ngayDiMuon = this.ngbDateStructToISOString(formValue.ngayDiMuon);
      dto.gioDuKienDen = this.combineDateTime(formValue.ngayDiMuon, formValue.gioDuKienDen);
    }
    
    if (loaiDon === LoaiDonYeuCau.CongTac) {
      dto.diaDiemCongTac = formValue.diaDiemCongTac?.trim();
      dto.mucDichCongTac = formValue.mucDichCongTac?.trim();
    }
    
    return dto;
  }
  
  /**
   * Close modal
   */
  close(): void {
    if (this.isDirty) {
      const modalRef = this.modal.open(ConfirmDialogComponent, {
        centered: true,
        backdrop: 'static'
      });

      modalRef.result.then(
        (confirmed) => {
          if (confirmed) {
            // Blur any focused element to prevent accessibility warnings
            if (document.activeElement instanceof HTMLElement) {
              document.activeElement.blur();
            }
            // Use setTimeout to ensure blur completes before dismissing
            setTimeout(() => {
              this.activeModal.dismiss('closed');
            }, 0);
          }
        },
        () => {} // Dismissed - do nothing
      );
    } else {
      // Blur any focused element to prevent accessibility warnings
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
      // Use setTimeout to ensure blur completes before dismissing
      setTimeout(() => {
        this.activeModal.dismiss('closed');
      }, 0);
    }
  }
  
  /**
   * Get current selected loaiDon
   */
  getCurrentLoaiDon(): LoaiDonYeuCau | null {
    return this.donForm.get('loaiDon')?.value;
  }
  
  /**
   * Check if should use single date picker (for half-day or one-day leave)
   */
  shouldUseSingleDatePicker(): boolean {
    const loaiDon = this.getCurrentLoaiDon();
    if (loaiDon !== LoaiDonYeuCau.NghiPhep) return false;
    
    const loaiNghiPhep = this.donForm.get('loaiNghiPhep')?.value;
    return loaiNghiPhep === LoaiNghiPhep.BuoiSang ||
           loaiNghiPhep === LoaiNghiPhep.BuoiChieu ||
           loaiNghiPhep === LoaiNghiPhep.MotNgay;
  }
  
  /**
   * Check if should use range date picker (for multi-day leave or business trip)
   */
  shouldUseRangeDatePicker(): boolean {
    const loaiDon = this.getCurrentLoaiDon();
    if (loaiDon === LoaiDonYeuCau.CongTac) return true;
    if (loaiDon !== LoaiDonYeuCau.NghiPhep) return false;
    
    const loaiNghiPhep = this.donForm.get('loaiNghiPhep')?.value;
    return loaiNghiPhep === LoaiNghiPhep.NhieuNgay;
  }
  
  /**
   * Handle loaiNghiPhep change - reset date fields only if switching between picker types
   */
  private onLoaiNghiPhepChange(loaiNghiPhep: LoaiNghiPhep | null): void {
    if (!loaiNghiPhep) return;
    
    const isSingleDateType = loaiNghiPhep === LoaiNghiPhep.BuoiSang || 
                             loaiNghiPhep === LoaiNghiPhep.BuoiChieu || 
                             loaiNghiPhep === LoaiNghiPhep.MotNgay;
    const isRangeDateType = loaiNghiPhep === LoaiNghiPhep.NhieuNgay;
    
    // Only reset if switching between single date and range date picker
    if (isSingleDateType && (this.fromDate || this.toDate)) {
      // Switching from range to single - convert first date to single date
      if (this.fromDate) {
        this.ngayNghi = this.fromDate;
        this.donForm.patchValue({
          ngayBatDau: this.fromDate,
          ngayKetThuc: this.fromDate
        }, { emitEvent: false });
      }
      this.fromDate = null;
      this.toDate = null;
    } else if (isRangeDateType && this.ngayNghi) {
      // Switching from single to range - convert single date to range start
      this.fromDate = this.ngayNghi;
      this.toDate = null;
      this.donForm.patchValue({
        ngayBatDau: this.ngayNghi,
        ngayKetThuc: null
      }, { emitEvent: false });
      this.ngayNghi = null;
    }
  }
  
  /**
   * Handle single date selection
   */
  onSingleDateSelection(date: NgbDate): void {
    this.ngayNghi = date;
    // Set both ngayBatDau and ngayKetThuc to the same date
    this.donForm.patchValue({
      ngayBatDau: date,
      ngayKetThuc: date
    });
  }
  
  /**
   * Check if field should be shown
   */
  shouldShowField(field: string): boolean {
    const loaiDon = this.getCurrentLoaiDon();
    if (!loaiDon) return false;
    
    switch (field) {
      case 'loaiNghiPhep':
        return loaiDon === LoaiDonYeuCau.NghiPhep;
      case 'ngayBatDau':
      case 'ngayKetThuc':
        return loaiDon === LoaiDonYeuCau.NghiPhep || loaiDon === LoaiDonYeuCau.CongTac;
      case 'ngayLamThem':
      case 'soGioLamThem':
        return loaiDon === LoaiDonYeuCau.LamThemGio;
      case 'ngayDiMuon':
      case 'gioDuKienDen':
        return loaiDon === LoaiDonYeuCau.DiMuon;
      case 'diaDiemCongTac':
      case 'mucDichCongTac':
        return loaiDon === LoaiDonYeuCau.CongTac;
      default:
        return false;
    }
  }
  
  // ============================================================================
  // Helper Methods
  // ============================================================================
  
  /**
   * Range datepicker methods
   */
  onDateSelection(date: NgbDate) {
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (this.fromDate && !this.toDate && date && date.after(this.fromDate)) {
      this.toDate = date;
    } else {
      this.toDate = null;
      this.fromDate = date;
    }
    
    // Update form values
    if (this.fromDate) {
      this.donForm.patchValue({ ngayBatDau: this.fromDate });
    }
    if (this.toDate) {
      this.donForm.patchValue({ ngayKetThuc: this.toDate });
    }
  }

  isHovered(date: NgbDate): boolean {
    return (
      !!this.fromDate && !this.toDate && !!this.hoveredDate && 
      date.after(this.fromDate) && date.before(this.hoveredDate)
    );
  }

  isInside(date: NgbDate): boolean {
    return !!this.toDate && date.after(this.fromDate!) && date.before(this.toDate);
  }

  isRange(date: NgbDate): boolean {
    return (
      date.equals(this.fromDate) ||
      (!!this.toDate && date.equals(this.toDate)) ||
      this.isInside(date) ||
      this.isHovered(date)
    );
  }
  
  /**
   * Mark dates as disabled (for ngbDatepicker)
   * Disable dates that are already FULLY booked (nghỉ cả ngày)
   * Nếu chỉ nghỉ nửa ngày (sáng hoặc chiều) thì KHÔNG disable
   */
  isDisabled = (date: NgbDate, current?: { month: number; year: number }) => {
    const info = this.getRequestedLeaveInfo(date);
    if (!info) return false;
    
    // Chỉ disable nếu nghỉ cả ngày
    return info.nghiCaNgay;
  }
  
  /**
   * Get thông tin ngày đã nghỉ cho một date cụ thể
   */
  getRequestedLeaveInfo(date: NgbDate): NgayNghiInfo | null {
    const dateKey = `${date.year}-${String(date.month).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`;
    return this.requestedLeaveDates.get(dateKey) || null;
  }
  
  /**
   * Check if a date is already requested for leave (bất kỳ loại nào)
   * Dùng để styling (bôi vàng)
   */
  isRequestedLeaveDate(date: NgbDate): boolean {
    const dateKey = `${date.year}-${String(date.month).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`;
    return this.requestedLeaveDates.has(dateKey);
  }
  
  /**
   * Check if a date is half-day leave (nghỉ nửa ngày)
   * Dùng để styling khác biệt (vàng nhạt hơn)
   */
  isHalfDayLeave(date: NgbDate): boolean {
    const info = this.getRequestedLeaveInfo(date);
    if (!info) return false;
    
    // Nửa ngày = chỉ nghỉ sáng HOẶC chỉ nghỉ chiều (không nghỉ cả ngày)
    return !info.nghiCaNgay && (info.buoiSang || info.buoiChieu);
  }

  validateInput(currentValue: NgbDate | null, input: string): NgbDate | null {
    const parsed = this.formatter.parse(input);
    return parsed && this.calendar.isValid(NgbDate.from(parsed)) ? NgbDate.from(parsed) : currentValue;
  }
  
  private dateToNgbDateStruct(date: Date | string): NgbDateStruct {
    const d = typeof date === 'string' ? new Date(date) : date;
    return {
      year: d.getFullYear(),
      month: d.getMonth() + 1,
      day: d.getDate()
    };
  }
  
  private dateToTimeStruct(date: Date | string): { hour: number; minute: number } {
    const d = typeof date === 'string' ? new Date(date) : date;
    return {
      hour: d.getHours(),
      minute: d.getMinutes()
    };
  }
  
  private ngbDateStructToISOString(date: NgbDateStruct | null): string | undefined {
    if (!date) return undefined;
    return `${date.year}-${String(date.month).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`;
  }
  
  private combineDateTime(date: NgbDateStruct, time: { hour: number; minute: number }): string {
    const dateStr = this.ngbDateStructToISOString(date);
    if (!dateStr) return new Date().toISOString();
    
    const combined = new Date(`${dateStr}T${String(time.hour).padStart(2, '0')}:${String(time.minute).padStart(2, '0')}:00`);
    return combined.toISOString();
  }
  
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
  
  /**
   * Check if field has error
   */
  hasError(fieldName: string, errorType?: string): boolean {
    const field = this.donForm.get(fieldName);
    if (!field) return false;
    
    if (errorType) {
      return field.hasError(errorType) && (field.dirty || field.touched);
    }
    return field.invalid && (field.dirty || field.touched);
  }
  
  /**
   * Get error message for field
   */
  getErrorMessage(fieldName: string): string {
    const field = this.donForm.get(fieldName);
    if (!field || !field.errors) return '';
    
    if (field.hasError('required')) return 'Trường này là bắt buộc';
    if (field.hasError('maxlength')) {
      const maxLength = field.errors['maxlength'].requiredLength;
      return `Không được vượt quá ${maxLength} ký tự`;
    }
    if (field.hasError('min')) return `Giá trị tối thiểu là ${field.errors['min'].min}`;
    if (field.hasError('max')) return `Giá trị tối đa là ${field.errors['max'].max}`;
    
    return 'Giá trị không hợp lệ';
  }
}
