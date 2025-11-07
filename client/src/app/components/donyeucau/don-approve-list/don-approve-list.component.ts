import { Component, inject, OnInit, signal, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Subject, takeUntil, finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { SpinnerService } from '../../../services/spinner.service';
import { DonYeuCauDto, LoaiDonYeuCau, TrangThaiDon } from '../../../types/don.model';
import { DonStatusBadgeComponent } from '../../../shared/don-status-badge/don-status-badge.component';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';
import { DonDetailComponent } from '../don-detail/don-detail.component';
import { DonApproveModalComponent, ApprovalResult } from '../../../shared/don-approve-modal/don-approve-modal.component';

@Component({
  selector: 'app-don-approve-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NgbPaginationModule,
    DonStatusBadgeComponent,
    LocalDatePipe
  ],
  templateUrl: './don-approve-list.component.html',
  styleUrl: './don-approve-list.component.css'
})
export class DonApproveListComponent implements OnInit, OnDestroy {
  private modal = inject(NgbModal);
  private donService = inject(DonYeuCauService);
  private spinner = inject(SpinnerService);
  private toastr = inject(ToastrService);
  private ngZone = inject(NgZone);
  
  // Data
  dons = signal<DonYeuCauDto[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  pendingCount = signal<number>(0);
  
  // Pagination
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  // Expose enums to template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  readonly TrangThaiDon = TrangThaiDon;
  readonly Math = Math;
  
  private destroy$ = new Subject<void>();
  
  ngOnInit(): void {
    this.loadDons();
    this.loadPendingCount();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Load danh sách đơn cần duyệt
   */
  loadDons(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.spinner.show('Đang tải danh sách đơn cần duyệt...');
    
    this.donService.getDonCanDuyet(
      this.pageNumber(),
      this.pageSize()
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
   * Load số lượng đơn đang chờ duyệt
   */
  loadPendingCount(): void {
    this.donService.countDonChoDuyet()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.pendingCount.set(result.count);
        },
        error: (error) => {
          console.error('Error loading pending count:', error);
        }
      });
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
    this.loadPendingCount();
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
   * Open approve modal (Chấp thuận)
   */
  approveDon(don: DonYeuCauDto): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(DonApproveModalComponent, {
            size: 'md',
            backdrop: 'static'
          });
          
          modalRef.componentInstance.don = don;
          
          modalRef.result.then(
            (result: ApprovalResult) => {
              if (result && result.trangThai === TrangThaiDon.DaChapThuan) {
                this.handleApproval(don.id, result.ghiChuNguoiDuyet);
              }
            },
            () => {
              // Modal dismissed
            }
          );
        });
      }, 0);
    });
  }
  
  /**
   * Open reject modal (Từ chối)
   */
  rejectDon(don: DonYeuCauDto): void {
    // Blur the trigger button to prevent focus issues
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    
    // Use setTimeout to ensure blur completes before opening modal
    this.ngZone.runOutsideAngular(() => {
      setTimeout(() => {
        this.ngZone.run(() => {
          const modalRef = this.modal.open(DonApproveModalComponent, {
            size: 'md',
            backdrop: 'static'
          });
          
          modalRef.componentInstance.don = don;
          
          modalRef.result.then(
            (result: ApprovalResult) => {
              if (result && result.trangThai === TrangThaiDon.BiTuChoi) {
                this.handleRejection(don.id, result.ghiChuNguoiDuyet);
              }
            },
            () => {
              // Modal dismissed
            }
          );
        });
      }, 0);
    });
  }
  
  /**
   * Handle approval
   */
  private handleApproval(donId: string, ghiChu?: string): void {
    this.spinner.show('Đang chấp thuận đơn...');
    
    this.donService.chapThuan(donId, ghiChu)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.spinner.hide())
      )
      .subscribe({
        next: () => {
          this.toastr.success('Đã chấp thuận đơn yêu cầu', 'Thành công');
          this.refresh();
        },
        error: (error) => {
          this.toastr.error(
            error.error?.message || 'Không thể chấp thuận đơn. Vui lòng thử lại.',
            'Lỗi'
          );
          console.error('Error approving don:', error);
        }
      });
  }
  
  /**
   * Handle rejection
   */
  private handleRejection(donId: string, ghiChu?: string): void {
    this.spinner.show('Đang từ chối đơn...');
    
    this.donService.tuChoi(donId, ghiChu)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.spinner.hide())
      )
      .subscribe({
        next: () => {
          this.toastr.success('Đã từ chối đơn yêu cầu', 'Thành công');
          this.refresh();
        },
        error: (error) => {
          this.toastr.error(
            error.error?.message || 'Không thể từ chối đơn. Vui lòng thử lại.',
            'Lỗi'
          );
          console.error('Error rejecting don:', error);
        }
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
}
