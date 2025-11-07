import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { DonYeuCauService } from '../../../services/don-yeu-cau.service';
import { RoleService } from '../../../services/role.service';
import { ThongKeDonYeuCauDto } from '../../../types/don.model';
import { FormsModule } from '@angular/forms';

/**
 * Component hiển thị thống kê đơn yêu cầu với charts
 * - Nhân viên: Thống kê đơn của tôi
 * - Trưởng Phòng: Thống kê đơn phòng ban
 * - Giám Đốc: Thống kê toàn công ty
 */
@Component({
  selector: 'app-don-stats',
  standalone: true,
  imports: [CommonModule, BaseChartDirective, FormsModule],
  templateUrl: './don-stats.component.html',
  styleUrl: './don-stats.component.css'
})
export class DonStatsComponent implements OnInit {
  private readonly donService = inject(DonYeuCauService);
  private readonly roleService = inject(RoleService);

  // Signals
  stats = signal<ThongKeDonYeuCauDto | null>(null);
  isLoading = signal(false);
  
  // Date range filter
  fromDate = signal<string>('');
  toDate = signal<string>('');

  // Role checks
  isGiamDoc = this.roleService.isGiamDoc;
  isTruongPhong = this.roleService.isTruongPhong;

  // ============================================================================
  // Chart 1: Trạng thái đơn (Doughnut Chart)
  // ============================================================================
  
  public doughnutChartLabels = ['Đã Chấp Thuận', 'Bị Từ Chối', 'Đang Chờ Duyệt', 'Đã Hủy'];
  public readonly doughnutChartType = 'doughnut' as const;
  
  public doughnutChartData = computed<ChartData<'doughnut'>>(() => {
    const data = this.stats();
    if (!data) {
      return { labels: this.doughnutChartLabels, datasets: [] };
    }
    
    return {
      labels: this.doughnutChartLabels,
      datasets: [{
        data: [
          data.daChapThuan,
          data.biTuChoi,
          data.dangChoDuyet,
          data.daHuy
        ],
        backgroundColor: [
          'rgba(40, 167, 69, 0.8)',   // success - Đã chấp thuận
          'rgba(220, 53, 69, 0.8)',   // danger - Bị từ chối
          'rgba(255, 193, 7, 0.8)',   // warning - Đang chờ
          'rgba(108, 117, 125, 0.8)'  // secondary - Đã hủy
        ],
        borderColor: [
          'rgba(40, 167, 69, 1)',
          'rgba(220, 53, 69, 1)',
          'rgba(255, 193, 7, 1)',
          'rgba(108, 117, 125, 1)'
        ],
        borderWidth: 2
      }]
    };
  });

  public doughnutChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          padding: 15,
          font: { size: 12 }
        }
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const label = context.label || '';
            const value = context.parsed || 0;
            const total = this.stats()?.tongSoDon || 0;
            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
            return `${label}: ${value} đơn (${percentage}%)`;
          }
        }
      }
    }
  };

  // ============================================================================
  // Chart 2: Loại đơn (Bar Chart)
  // ============================================================================
  
  public barChartLabels = ['Nghỉ Phép', 'Làm Thêm Giờ', 'Đi Muộn', 'Công Tác'];
  public readonly barChartType = 'bar' as const;
  
  public barChartData = computed<ChartData<'bar'>>(() => {
    const data = this.stats();
    if (!data) {
      return { labels: this.barChartLabels, datasets: [] };
    }
    
    return {
      labels: this.barChartLabels,
      datasets: [{
        label: 'Số lượng đơn',
        data: [
          data.soDonNghiPhep,
          data.soDonLamThemGio,
          data.soDonDiMuon,
          data.soDonCongTac
        ],
        backgroundColor: [
          'rgba(54, 162, 235, 0.7)',  // blue
          'rgba(75, 192, 192, 0.7)',  // teal
          'rgba(255, 159, 64, 0.7)',  // orange
          'rgba(153, 102, 255, 0.7)'  // purple
        ],
        borderColor: [
          'rgba(54, 162, 235, 1)',
          'rgba(75, 192, 192, 1)',
          'rgba(255, 159, 64, 1)',
          'rgba(153, 102, 255, 1)'
        ],
        borderWidth: 2,
        borderRadius: 6
      }]
    };
  });

  public barChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          stepSize: 1,
          font: { size: 11 }
        }
      },
      x: {
        ticks: {
          font: { size: 11 }
        }
      }
    },
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        callbacks: {
          label: (context) => `${context.parsed.y} đơn`
        }
      }
    }
  };

  // ============================================================================
  // Lifecycle
  // ============================================================================

  ngOnInit(): void {
    this.loadStats();
  }

  // ============================================================================
  // Methods
  // ============================================================================

  /**
   * Load thống kê dựa vào role
   */
  loadStats(): void {
    this.isLoading.set(true);
    
    const from = this.fromDate() || undefined;
    const to = this.toDate() || undefined;

    let request;
    if (this.isGiamDoc()) {
      // Giám Đốc: Thống kê toàn công ty
      request = this.donService.thongKeToanCongTy(from, to);
    } else if (this.isTruongPhong()) {
      // TODO: Lấy phongBanId của Trưởng Phòng từ currentUser
      // Tạm thời dùng thống kê cá nhân
      request = this.donService.thongKeMyDons(from, to);
    } else {
      // Nhân viên: Thống kê đơn của tôi
      request = this.donService.thongKeMyDons(from, to);
    }

    request.subscribe({
      next: (data) => {
        this.stats.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Lỗi khi tải thống kê:', err);
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Apply date range filter
   */
  applyFilter(): void {
    this.loadStats();
  }

  /**
   * Reset filter
   */
  resetFilter(): void {
    this.fromDate.set('');
    this.toDate.set('');
    this.loadStats();
  }

  /**
   * Get title dựa vào role
   */
  getTitle(): string {
    if (this.isGiamDoc()) return 'Thống Kê Toàn Công Ty';
    if (this.isTruongPhong()) return 'Thống Kê Phòng Ban';
    return 'Thống Kê Đơn Của Tôi';
  }

  /**
   * Calculate percentage
   */
  getPercentage(value: number, total: number): string {
    if (total === 0) return '0.0';
    return ((value / total) * 100).toFixed(1);
  }
}
