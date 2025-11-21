import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { LichNghiDashboardDto } from '../../../types/lichnghi.model';
import { ToastrService } from 'ngx-toastr';
import { LOAI_DON_DISPLAY_NAMES, LoaiDonYeuCau } from '../../../types/don.model';

@Component({
  selector: 'app-lich-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lich-dashboard.component.html',
  styleUrl: './lich-dashboard.component.css'
})
export class LichDashboardComponent implements OnInit {
  private lichNghiService = inject(LichNghiService);
  private toastr = inject(ToastrService);

  dashboard = signal<LichNghiDashboardDto | null>(null);
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    
    this.lichNghiService.getMyDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Lỗi khi tải dashboard:', err);
        this.toastr.error('Không thể tải dữ liệu dashboard', 'Lỗi');
        this.isLoading.set(false);
      }
    });
  }

  getQuotaPercentage(): number {
    const quota = this.dashboard()?.quotaThangHienTai;
    if (!quota || quota.soNgayPhepThang === 0) return 0;
    return Math.min(100, (quota.soNgayDaSuDung / quota.soNgayPhepThang) * 100);
  }

  getQuotaBarClass(): string {
    const percentage = this.getQuotaPercentage();
    if (percentage >= 100) return 'bg-danger';
    if (percentage >= 80) return 'bg-warning';
    return 'bg-success';
  }

  getAlertClass(warning: string): string {
    if (warning.includes('vượt') || warning.includes('Vượt')) return 'alert-danger';
    if (warning.includes('sắp hết') || warning.includes('gần đạt')) return 'alert-warning';
    return 'alert-info';
  }

  formatDate(date?: Date | string): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  getCurrentYear(): number {
    return new Date().getFullYear();
  }

  getLoaiDonDisplayName(loaiDon: string): string {
    return LOAI_DON_DISPLAY_NAMES[loaiDon as LoaiDonYeuCau] || loaiDon;
  }
}
