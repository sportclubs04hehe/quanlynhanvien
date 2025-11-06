import { Component, inject, OnInit, signal, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject, takeUntil, finalize } from 'rxjs';
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
    DonStatusBadgeComponent,
    LocalDatePipe
  ],
  templateUrl: './don-my-list.component.html',
  styleUrl: './don-my-list.component.css'
})
export class DonMyListComponent implements OnInit, OnDestroy {
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
  selectedLoaiDon = signal<LoaiDonYeuCau | null>(null);
  selectedTrangThai = signal<TrangThaiDon | null>(null);
  
  // Expose enums to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly TrangThaiDon = TrangThaiDon;
  readonly Math = Math;
  
  private destroy$ = new Subject<void>();
  
  ngOnInit(): void {
    this.loadDons();
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
    
    this.donService.getMyDons(
      this.pageNumber(),
      this.pageSize(),
      this.selectedLoaiDon() || undefined,
      this.selectedTrangThai() || undefined
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
   * Filter thay đổi
   */
  onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadDons();
  }
  
  /**
   * Clear filters
   */
  clearFilters(): void {
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
   * Delete don
   */
  deleteDon(don: DonYeuCauDto): void {
    const modalRef = this.modal.open(ConfirmDialogComponent);
    modalRef.componentInstance.title = 'Xác Nhận Xóa';
    modalRef.componentInstance.message = `Bạn có chắc muốn xóa đơn "${don.loaiDonText}"? Hành động này không thể hoàn tác.`;
    modalRef.componentInstance.confirmText = 'Xóa';
    modalRef.componentInstance.confirmClass = 'btn-danger';
    
    modalRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.spinner.show('Đang xóa đơn...');
          this.donService.delete(don.id)
            .pipe(finalize(() => this.spinner.hide()))
            .subscribe({
              next: () => {
                this.loadDons();
              },
              error: (error) => {
                console.error('Error deleting don:', error);
                alert('Không thể xóa đơn. Vui lòng thử lại.');
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
    return this.selectedLoaiDon() !== null || this.selectedTrangThai() !== null;
  }
}
