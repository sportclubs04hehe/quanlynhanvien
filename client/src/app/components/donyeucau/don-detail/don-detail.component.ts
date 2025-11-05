import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { finalize } from 'rxjs';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { SpinnerService } from '../../../services/spinner.service';
import { DonYeuCauDto, LoaiDonYeuCau, getTrangThaiDonDisplayName, getLoaiDonDisplayName } from '../../../types/don.model';
import { DonStatusBadgeComponent } from '../../../shared/don-status-badge/don-status-badge.component';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';

@Component({
  selector: 'app-don-detail',
  standalone: true,
  imports: [CommonModule, DonStatusBadgeComponent, LocalDatePipe],
  templateUrl: './don-detail.component.html',
  styleUrl: './don-detail.component.css'
})
export class DonDetailComponent implements OnInit {
  @Input() donId!: string;
  
  private donService = inject(DonYeuCauService);
  private spinner = inject(SpinnerService);
  activeModal = inject(NgbActiveModal);
  
  don = signal<DonYeuCauDto | null>(null);
  errorMessage = signal<string | null>(null);
  
  // Expose enum for template
  readonly LoaiDonYeuCau = LoaiDonYeuCau;
  
  ngOnInit(): void {
    this.loadDonDetail();
  }
  
  /**
   * Load chi tiết đơn từ API
   */
  private loadDonDetail(): void {
    if (!this.donId) {
      this.errorMessage.set('Không tìm thấy thông tin đơn yêu cầu');
      return;
    }
    
    this.spinner.show('Đang tải chi tiết đơn...');
    this.donService.getById(this.donId)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: (don) => {
          this.don.set(don);
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải thông tin đơn yêu cầu');
          console.error('Error loading don detail:', error);
        }
      });
  }
  
  /**
   * Close modal
   */
  close(): void {
    this.activeModal.dismiss();
  }
  
  /**
   * Get status display name
   */
  getStatusDisplayName(don: DonYeuCauDto): string {
    return getTrangThaiDonDisplayName(don.trangThai);
  }
  
  /**
   * Get loai don display name
   */
  getLoaiDonDisplayName(don: DonYeuCauDto): string {
    return getLoaiDonDisplayName(don.loaiDon);
  }
  
  /**
   * Check if has approval info
   */
  hasApprovalInfo(don: DonYeuCauDto): boolean {
    return !!(don.tenNguoiDuyet || don.ngayDuyet || don.ghiChuNguoiDuyet);
  }
  
  /**
   * Format decimal to string
   */
  formatDecimal(value?: number | null): string {
    if (value == null) return 'N/A';
    return value.toString();
  }
}
