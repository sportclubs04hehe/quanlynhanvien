import { Component, inject, OnInit, signal, OnDestroy, NgZone, input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject, takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { ExportService } from '../../../services/export.service';
import { DonYeuCauDto, LoaiDonYeuCau, TrangThaiDon, canEditDon, canCancelDon } from '../../../types/don.model';
import { DonStatusBadgeComponent } from '../../../shared/don-status-badge/don-status-badge.component';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';
import { HighlightPipe } from '../../../shared/pipes/highlight.pipe';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';
import { DonDetailComponent } from '../don-detail/don-detail.component';
import { DonCreateEditComponent } from '../don-create-edit/don-create-edit.component';
import { SEARCH_DEBOUNCE_TIME } from '../../../shared/config/search.config';

@Component({
  selector: 'app-don-my-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    NgbPaginationModule,
    DonStatusBadgeComponent,
    LocalDatePipe,
    HighlightPipe
  ],
  templateUrl: './don-my-list.component.html',
  styleUrl: './don-my-list.component.css'
})
export class DonMyListComponent implements OnInit, OnDestroy, OnChanges {
  private modal = inject(NgbModal);
  private donService = inject(DonYeuCauService);
  private toastr = inject(ToastrService);
  private ngZone = inject(NgZone);
  private exportService = inject(ExportService);
  
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
  searchMaDon = signal<string>('');
  searchLyDo = signal<string>('');
  selectedLoaiDon = signal<LoaiDonYeuCau | null>(null);
  selectedTrangThai = signal<TrangThaiDon | null>(null);
  
  // Expose enums to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly TrangThaiDon = TrangThaiDon;
  readonly Math = Math;
  
  // Subject for search debounce
  private searchSubject$ = new Subject<string>();
  private destroy$ = new Subject<void>();
  
  ngOnInit(): void {
    // Apply initial filter nếu có
    const initialFilter = this.initialTrangThai();
    if (initialFilter !== null) {
      this.selectedTrangThai.set(initialFilter);
    }
    
    // Setup search debounce using shared config
    this.searchSubject$
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_TIME),
        distinctUntilChanged(),
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
    
    this.donService.getMyDons(
      this.pageNumber(),
      this.pageSize(),
      this.searchMaDon() || undefined,
      this.searchLyDo() || undefined,
      this.selectedLoaiDon() || undefined,
      this.selectedTrangThai() || undefined
    )
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
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
   * Called when search input changes (với debounce)
   */
  onSearchChange(): void {
    // Trigger debounce với giá trị kết hợp của cả 2 trường tìm kiếm
    this.searchSubject$.next(`${this.searchMaDon()}|${this.searchLyDo()}`);
  }
  
  /**
   * Filter thay đổi (dropdown - immediate)
   */
  onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadDons();
  }
  
  /**
   * Clear filters
   */
  clearFilters(): void {
    this.searchMaDon.set('');
    this.searchLyDo.set('');
    this.selectedLoaiDon.set(null);
    this.selectedTrangThai.set(null);
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
          this.donService.huyDon(don.id)
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
    return this.searchMaDon() !== '' || this.searchLyDo() !== '' || this.selectedLoaiDon() !== null || this.selectedTrangThai() !== null;
  }
  
  /**
   * Xuất danh sách đơn ra Excel (TOÀN BỘ dữ liệu, không phân trang)
   */
  exportToExcel(): void {
    
    this.donService.getMyDonsForExport(
      this.searchMaDon() || undefined,
      this.searchLyDo() || undefined,
      this.selectedLoaiDon() || undefined,
      this.selectedTrangThai() || undefined
    )
      .pipe(
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (allDons) => {
          if (allDons.length === 0) {
            this.toastr.warning('Không có dữ liệu để xuất!', 'Cảnh báo');
            return;
          }
          
          try {
            this.exportService.exportToExcel(allDons, 'DonCuaToi');
            this.toastr.success(`Đã xuất ${allDons.length} đơn ra Excel thành công!`, 'Thành công');
          } catch (error) {
            console.error('Error exporting to Excel:', error);
            this.toastr.error('Có lỗi xảy ra khi xuất Excel!', 'Lỗi');
          }
        },
        error: (error) => {
          console.error('Error loading data for export:', error);
          this.toastr.error('Không thể tải dữ liệu để xuất!', 'Lỗi');
        }
      });
  }
}