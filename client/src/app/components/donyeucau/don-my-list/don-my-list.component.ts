import { Component, inject, OnInit, signal, OnDestroy, NgZone, input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbPaginationModule, NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { Subject, takeUntil, finalize, debounceTime } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { SpinnerService } from '../../../services/spinner.service';
import { DonYeuCauDto, LoaiDonYeuCau, TrangThaiDon, canEditDon, canCancelDon } from '../../../types/don.model';
import { DonStatusBadgeComponent } from '../../../shared/don-status-badge/don-status-badge.component';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';
import { DonDetailComponent } from '../don-detail/don-detail.component';
import { DonCreateEditComponent } from '../don-create-edit/don-create-edit.component';

@Component({
  selector: 'app-don-my-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    NgbPaginationModule,
    NgbDatepickerModule,
    DonStatusBadgeComponent,
    LocalDatePipe
  ],
  templateUrl: './don-my-list.component.html',
  styleUrl: './don-my-list.component.css'
})
export class DonMyListComponent implements OnInit, OnDestroy, OnChanges {
  private modal = inject(NgbModal);
  private donService = inject(DonYeuCauService);
  private spinner = inject(SpinnerService);
  private toastr = inject(ToastrService);
  private ngZone = inject(NgZone);
  
  // Input: Initial filter từ parent component
  initialTrangThai = input<TrangThaiDon | null>(null);
  
  // Data
  dons = signal<DonYeuCauDto[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  
  // Pagination
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  // Filter - Using writable signals for ngModel compatibility
  selectedLoaiDon = signal<LoaiDonYeuCau | null>(null);
  selectedTrangThai = signal<TrangThaiDon | null>(null);
  searchTerm = signal<string>('');
  fromDate = signal<NgbDateStruct | null>(null);
  toDate = signal<NgbDateStruct | null>(null);
  
  // Search debounce
  private searchSubject$ = new Subject<string>();
  
  // Expose enums to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly TrangThaiDon = TrangThaiDon;
  readonly Math = Math;
  
  private destroy$ = new Subject<void>();
  
  ngOnInit(): void {
    // Apply initial filter nếu có
    const initialFilter = this.initialTrangThai();
    if (initialFilter !== null) {
      this.selectedTrangThai.set(initialFilter);
    }
    
    // Setup search debounce
    this.searchSubject$
      .pipe(
        debounceTime(500),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.pageNumber.set(1);
        this.loadDons();
      });
    
    this.loadDons();
  }
  
  ngOnChanges(changes: SimpleChanges): void {
    // Khi initialTrangThai thay đổi từ parent
    if (changes['initialTrangThai'] && !changes['initialTrangThai'].firstChange) {
      const newValue = changes['initialTrangThai'].currentValue;
      
      if (newValue === null) {
        // Reset filter khi parent gửi null
        this.clearFilters();
      } else if (newValue !== undefined) {
        // Apply filter mới
        this.selectedTrangThai.set(newValue);
        this.loadDons();
      }
    }
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Load danh sách đơn của tôi
   */
  loadDons(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.spinner.show('Đang tải danh sách đơn...');
    
    // Convert NgbDateStruct to Date
    const fromDateValue = this.fromDate() ? this.ngbDateStructToDate(this.fromDate()!) : undefined;
    const toDateValue = this.toDate() ? this.ngbDateStructToDate(this.toDate()!) : undefined;
    
    this.donService.getMyDons(
      this.pageNumber(),
      this.pageSize(),
      this.selectedLoaiDon() || undefined,
      this.selectedTrangThai() || undefined,
      this.searchTerm() || undefined,
      fromDateValue,
      toDateValue
    )
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.spinner.hide();
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: (result) => {
          this.dons.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages || Math.ceil(result.totalCount / result.pageSize));
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải danh sách đơn');
          console.error('Error loading dons:', error);
        }
      });
  }
  
  /**
   * Search change (debounced)
   */
  onSearchChange(): void {
    this.searchSubject$.next(this.searchTerm());
  }
  
  /**
   * Filter thay đổi
   */
  onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadDons();
  }
  
  /**
   * Apply quick filters
   */
  applyQuickFilter(type: 'thisWeek' | 'thisMonth' | 'pending' | 'rejected'): void {
    const now = new Date();
    
    switch (type) {
      case 'thisWeek':
        const startOfWeek = new Date(now);
        startOfWeek.setDate(now.getDate() - now.getDay() + 1); // Monday
        this.fromDate.set(this.dateToNgbDateStruct(startOfWeek));
        this.toDate.set(this.dateToNgbDateStruct(now));
        this.selectedTrangThai.set(null);
        break;
        
      case 'thisMonth':
        const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
        this.fromDate.set(this.dateToNgbDateStruct(startOfMonth));
        this.toDate.set(this.dateToNgbDateStruct(now));
        this.selectedTrangThai.set(null);
        break;
        
      case 'pending':
        this.selectedTrangThai.set(TrangThaiDon.DangChoDuyet);
        this.fromDate.set(null);
        this.toDate.set(null);
        break;
        
      case 'rejected':
        this.selectedTrangThai.set(TrangThaiDon.BiTuChoi);
        this.fromDate.set(null);
        this.toDate.set(null);
        break;
    }
    
    this.pageNumber.set(1);
    this.loadDons();
  }
  
  /**
   * Clear filters
   */
  clearFilters(): void {
    this.selectedLoaiDon.set(null);
    this.selectedTrangThai.set(null);
    this.searchTerm.set('');
    this.fromDate.set(null);
    this.toDate.set(null);
    this.pageNumber.set(1);
    this.loadDons();
  }
  
  /**
   * Pagination change
   */
  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.loadDons();
  }
  
  /**
   * Check if don can be edited
   */
  canEdit(don: DonYeuCauDto): boolean {
    return canEditDon(don.trangThai);
  }
  
  /**
   * Check if don can be cancelled
   */
  canCancel(don: DonYeuCauDto): boolean {
    return canCancelDon(don.trangThai);
  }
  
  /**
   * Open detail modal
   */
  viewDetail(don: DonYeuCauDto): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(DonDetailComponent, { 
            size: 'lg',
            backdrop: 'static'
          });
          modalRef.componentInstance.donId = don.id;
        });
      }, 0);
    });
  }
  
  /**
   * Open create modal
   */
  createDon(): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(DonCreateEditComponent, {
            size: 'lg',
            backdrop: 'static',
            keyboard: true
          });

          modalRef.componentInstance.mode = 'create';

          modalRef.result.then(
            (result: DonYeuCauDto) => {
              if (result) {
                this.toastr.success('Tạo đơn yêu cầu thành công!', 'Thành công');
                this.loadDons(); // Reload list
              }
            },
            (reason) => {}
          );
        });
      }, 0);
    });
  }
  
  /**
   * Open edit modal
   */
  editDon(don: DonYeuCauDto): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(DonCreateEditComponent, {
            size: 'lg',
            backdrop: 'static',
            keyboard: true
          });

          modalRef.componentInstance.mode = 'edit';
          modalRef.componentInstance.donId = don.id;

          modalRef.result.then(
            (result: DonYeuCauDto) => {
              if (result) {
                this.toastr.success('Cập nhật đơn yêu cầu thành công!', 'Thành công');
                this.loadDons(); // Reload list
              }
            },
            (reason) => {
              // Modal dismissed
              console.log('Modal dismissed:', reason);
            }
          );
        });
      }, 0);
    });
  }
  
  /**
   * Cancel don (Hủy đơn)
   */
  cancelDon(don: DonYeuCauDto): void {
    const modalRef = this.modal.open(ConfirmDialogComponent);
    modalRef.componentInstance.title = 'Xác Nhận Hủy Đơn';
    modalRef.componentInstance.message = `Bạn có chắc muốn hủy đơn "${don.loaiDonText}"?`;
    modalRef.componentInstance.confirmText = 'Hủy Đơn';
    modalRef.componentInstance.confirmClass = 'btn-warning';
    
    modalRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.spinner.show('Đang hủy đơn...');
          this.donService.huyDon(don.id)
            .pipe(finalize(() => this.spinner.hide()))
            .subscribe({
              next: () => {
                this.loadDons();
              },
              error: (error) => {
                console.error('Error cancelling don:', error);
                alert('Không thể hủy đơn. Vui lòng thử lại.');
              }
            });
        }
      },
      () => {}
    );
  }
  
  /**
   * Check if any filter is active
   */
  hasActiveFilters(): boolean {
    return this.selectedLoaiDon() !== null || 
           this.selectedTrangThai() !== null ||
           this.searchTerm() !== '' ||
           this.fromDate() !== null ||
           this.toDate() !== null;
  }
  
  /**
   * Format date range for Nghỉ Phép/Công Tác
   * Format: 01/12 - 05/12 (5 ngày)
   */
  formatDateRange(ngayBatDau?: Date | string, ngayKetThuc?: Date | string, soNgay?: number): string {
    if (!ngayBatDau || !ngayKetThuc) return '-';
    
    const start = typeof ngayBatDau === 'string' ? new Date(ngayBatDau) : ngayBatDau;
    const end = typeof ngayKetThuc === 'string' ? new Date(ngayKetThuc) : ngayKetThuc;
    
    const startStr = `${this.padZero(start.getDate())}/${this.padZero(start.getMonth() + 1)}`;
    const endStr = `${this.padZero(end.getDate())}/${this.padZero(end.getMonth() + 1)}`;
    
    return `${startStr} - ${endStr}${soNgay ? ` (${soNgay} ngày)` : ''}`;
  }
  
  /**
   * Format time for Đi Muộn
   * Format: 09:30
   */
  formatTime(datetime?: Date | string): string {
    if (!datetime) return '-';
    
    const date = typeof datetime === 'string' ? new Date(datetime) : datetime;
    return `${this.padZero(date.getHours())}:${this.padZero(date.getMinutes())}`;
  }
  
  /**
   * Helper: Pad zero
   */
  private padZero(num: number): string {
    return num < 10 ? `0${num}` : `${num}`;
  }
  
  /**
   * Convert NgbDateStruct to Date
   */
  private ngbDateStructToDate(dateStruct: NgbDateStruct): Date {
    return new Date(dateStruct.year, dateStruct.month - 1, dateStruct.day);
  }
  
  /**
   * Convert Date to NgbDateStruct
   */
  private dateToNgbDateStruct(date: Date): NgbDateStruct {
    return {
      year: date.getFullYear(),
      month: date.getMonth() + 1,
      day: date.getDate()
    };
  }
}
