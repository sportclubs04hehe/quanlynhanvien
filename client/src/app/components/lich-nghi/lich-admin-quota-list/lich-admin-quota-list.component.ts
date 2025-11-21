import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { PhongbanService } from '../../../services/phongban.service';
import { NghiPhepQuotaDto } from '../../../types/lichnghi.model';
import { PhongBanDto } from '../../../types/phongban.model';
import { LichAdminQuotaEditComponent } from '../lich-admin-quota-edit/lich-admin-quota-edit.component';
import { LichAdminQuotaBulkComponent } from '../lich-admin-quota-bulk/lich-admin-quota-bulk.component';

@Component({
  selector: 'app-lich-admin-quota-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LichAdminQuotaEditComponent, LichAdminQuotaBulkComponent],
  templateUrl: './lich-admin-quota-list.component.html',
  styleUrl: './lich-admin-quota-list.component.css'
})
export class LichAdminQuotaListComponent implements OnInit {
  private lichNghiService = inject(LichNghiService);
  private phongBanService = inject(PhongbanService);
  private toastr = inject(ToastrService);

  // Data
  quotas = signal<NghiPhepQuotaDto[]>([]);
  phongBans = signal<PhongBanDto[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  // Modal state
  showEditModal = signal(false);
  selectedQuota = signal<NghiPhepQuotaDto | null>(null);
  showBulkModal = signal(false);

  // Filter state
  selectedNam = signal(new Date().getFullYear());
  selectedThang = signal(new Date().getMonth() + 1); // 1-12
  selectedPhongBanId = signal<string>('all');

  // Computed: Statistics
  stats = computed(() => {
    const quotas = this.quotas();
    return {
      tongSoNhanVien: quotas.length,
      soNguoiVuotHanMuc: quotas.filter(q => q.daVuotQuota).length,
      soNguoiSapHet: quotas.filter(q => !q.daVuotQuota && q.soNgayPhepConLai < 0.5).length,
      tongNgayDaSuDung: quotas.reduce((sum, q) => sum + q.soNgayDaSuDung, 0)
    };
  });

  // Month/Year options
  readonly years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - 2 + i);
  readonly months = [
    { value: 1, label: 'Tháng 1' },
    { value: 2, label: 'Tháng 2' },
    { value: 3, label: 'Tháng 3' },
    { value: 4, label: 'Tháng 4' },
    { value: 5, label: 'Tháng 5' },
    { value: 6, label: 'Tháng 6' },
    { value: 7, label: 'Tháng 7' },
    { value: 8, label: 'Tháng 8' },
    { value: 9, label: 'Tháng 9' },
    { value: 10, label: 'Tháng 10' },
    { value: 11, label: 'Tháng 11' },
    { value: 12, label: 'Tháng 12' }
  ];

  ngOnInit(): void {
    this.loadPhongBans();
    this.loadQuotas();
  }

  /**
   * Load danh sách phòng ban cho filter dropdown
   */
  loadPhongBans(): void {
    this.phongBanService.getAll(1, 100).subscribe({
      next: (result) => {
        this.phongBans.set(result.items);
      },
      error: (error) => {
        console.error('Error loading phong bans:', error);
      }
    });
  }

  /**
   * Load danh sách quota
   */
  loadQuotas(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    const phongBanId = this.selectedPhongBanId() === 'all' ? undefined : this.selectedPhongBanId();

    this.lichNghiService.getAllQuotas(
      this.selectedNam(),
      this.selectedThang(),
      phongBanId
    ).subscribe({
      next: (data) => {
        this.quotas.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading quotas:', error);
        this.errorMessage.set(error.error?.message || 'Không thể tải danh sách hạn mức nghỉ phép');
        this.toastr.error('Không thể tải danh sách hạn mức nghỉ phép', 'Lỗi');
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Handle filter change
   */
  onFilterChange(): void {
    this.loadQuotas();
  }

  /**
   * Get status badge class based on leave quota
   */
  getStatusBadgeClass(quota: NghiPhepQuotaDto): string {
    if (quota.daVuotQuota) {
      return 'badge bg-danger'; // Đỏ - Vượt hạn mức
    } else if (quota.soNgayPhepConLai < 0.5) {
      return 'badge bg-warning text-dark'; // Vàng - Sắp hết
    } else {
      return 'badge bg-success'; // Xanh - OK
    }
  }

  /**
   * Get status text
   */
  getStatusText(quota: NghiPhepQuotaDto): string {
    if (quota.daVuotQuota) {
      return 'Vượt hạn mức';
    } else if (quota.soNgayPhepConLai < 0.5) {
      return 'Sắp hết phép';
    } else {
      return 'Bình thường';
    }
  }

  /**
   * Format decimal number
   */
  formatNumber(value: number): string {
    return value % 1 === 0 ? value.toFixed(0) : value.toFixed(1);
  }

  /**
   * Edit quota - Mở modal chỉnh sửa
   */
  editQuota(quota: NghiPhepQuotaDto): void {
    this.selectedQuota.set(quota);
    this.showEditModal.set(true);
  }

  /**
   * Refresh list
   */
  refresh(): void {
    this.loadQuotas();
  }

  /**
   * Handle modal saved event
   */
  onQuotaSaved(): void {
    this.showEditModal.set(false);
    this.selectedQuota.set(null);
    this.loadQuotas(); // Reload data
  }

  /**
   * Handle modal cancelled event
   */
  onModalCancelled(): void {
    this.showEditModal.set(false);
    this.selectedQuota.set(null);
  }

  /**
   * Open bulk configuration modal
   */
  openBulkModal(): void {
    this.showBulkModal.set(true);
  }

  /**
   * Handle bulk configuration saved
   */
  onBulkSaved(): void {
    this.showBulkModal.set(false);
    this.loadQuotas(); // Reload data
  }

  /**
   * Handle bulk modal cancelled
   */
  onBulkCancelled(): void {
    this.showBulkModal.set(false);
  }
}
