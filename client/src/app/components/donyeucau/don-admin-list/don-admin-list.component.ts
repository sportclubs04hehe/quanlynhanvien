import { Component, inject, OnInit, signal, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject, takeUntil, finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { SpinnerService } from '../../../services/spinner.service';
import { DonYeuCauDto, FilterDonYeuCauDto, LoaiDonYeuCau, TrangThaiDon } from '../../../types/don.model';
import { DonStatusBadgeComponent } from '../../../shared/don-status-badge/don-status-badge.component';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';
import { DonDetailComponent } from '../don-detail/don-detail.component';
import { ConfirmDialogComponent } from '../../../shared/modal/confirm-dialog/confirm-dialog.component';
import { DonFilterComponent } from '../../../shared/don-filter/don-filter.component';

@Component({
  selector: 'app-don-admin-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NgbPaginationModule,
    DonStatusBadgeComponent,
    LocalDatePipe,
    DonFilterComponent
  ],
  templateUrl: './don-admin-list.component.html',
  styleUrl: './don-admin-list.component.css'
})
export class DonAdminListComponent implements OnInit, OnDestroy {
  private modal = inject(NgbModal);
  private donService = inject(DonYeuCauService);
  private spinner = inject(SpinnerService);
  private toastr = inject(ToastrService);
  private ngZone = inject(NgZone);
  
  // Data
  dons = signal<DonYeuCauDto[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  
  // Pagination
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  // Filter
  filter = signal<FilterDonYeuCauDto>({
    pageNumber: 1,
    pageSize: 10
  });
  
  // Expose enums to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly TrangThaiDon = TrangThaiDon;
  readonly Math = Math;
  
  private destroy$ = new Subject<void>();
  
  ngOnInit(): void {
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Load danh sách đơn ĐÃ XỬ LÝ (Admin view - Giám Đốc)
   * Backend tự động filter: Chỉ trả về DaChapThuan, BiTuChoi, DaHuy
   * Không bao gồm: DangChoDuyet (để Trưởng Phòng xử lý theo workflow)
   */
  loadDons(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.spinner.show('Đang tải danh sách đơn...');
    
    // Build filter with current pagination
    const currentFilter: FilterDonYeuCauDto = {
      ...this.filter(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    };
    
    // Use getProcessedDons() instead of getAll()
    // Backend will automatically exclude DangChoDuyet
    this.donService.getProcessedDons(currentFilter)
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
   * Handle filter change from DonFilterComponent
   */
  onFilterChange(newFilter: FilterDonYeuCauDto): void {
    this.filter.set(newFilter);
    this.pageNumber.set(1); // Reset to first page
    this.loadDons();
  }
  
  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.filter.set({
      pageNumber: 1,
      pageSize: 10
    });
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
   * Refresh list
   */
  refresh(): void {
    this.pageNumber.set(1);
    this.loadDons();
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
   * Delete don (Admin can delete for audit/compliance reasons)
   * Note: Backend should implement soft delete to preserve history
   */
  deleteDon(don: DonYeuCauDto): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(ConfirmDialogComponent);
          modalRef.componentInstance.title = 'Xác Nhận Xóa';
          modalRef.componentInstance.message = `Bạn có chắc muốn xóa đơn "${don.loaiDonText}" của ${don.tenNhanVien}? Hành động này không thể hoàn tác.`;
          modalRef.componentInstance.confirmText = 'Xóa';
          modalRef.componentInstance.confirmClass = 'btn-danger';
          
          modalRef.result.then(
            (confirmed) => {
              if (confirmed) {
                this.spinner.show('Đang xóa đơn...');
                this.donService.delete(don.id)
                  .pipe(
                    takeUntil(this.destroy$),
                    finalize(() => this.spinner.hide())
                  )
                  .subscribe({
                    next: () => {
                      this.toastr.success('Đã xóa đơn yêu cầu', 'Thành công');
                      this.loadDons();
                    },
                    error: (error) => {
                      this.toastr.error(
                        error.error?.message || 'Không thể xóa đơn. Vui lòng thử lại.',
                        'Lỗi'
                      );
                      console.error('Error deleting don:', error);
                    }
                  });
              }
            },
            () => {}
          );
        });
      }, 0);
    });
  }
  
  /**
   * Get badge class for loai don
   */
  getLoaiDonBadgeClass(loaiDon: LoaiDonYeuCau): string {
    switch (loaiDon) {
      case LoaiDonYeuCau.NghiPhep:
        return 'bg-primary';
      case LoaiDonYeuCau.LamThemGio:
        return 'bg-success';
      case LoaiDonYeuCau.DiMuon:
        return 'bg-warning text-dark';
      case LoaiDonYeuCau.CongTac:
        return 'bg-info';
      default:
        return 'bg-secondary';
    }
  }
  
  /**
   * Check if filter is active
   */
  hasActiveFilters(): boolean {
    const f = this.filter();
    return !!(
      f.searchTerm ||
      f.loaiDon !== undefined ||
      f.trangThai !== undefined ||
      f.nhanVienId ||
      f.nguoiDuyetId ||
      f.phongBanId ||
      f.tuNgay ||
      f.denNgay
    );
  }
}
