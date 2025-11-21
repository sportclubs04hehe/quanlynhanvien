import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { DonYeuCauDto } from '../../../types/don.model';
import { ToastrService } from 'ngx-toastr';

interface FilterOptions {
  loaiDon: string;
  trangThai: string;
  sortBy: 'ngayBatDau' | 'ngayTao';
  sortOrder: 'asc' | 'desc';
}

@Component({
  selector: 'app-lich-upcoming',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lich-upcoming.component.html',
  styleUrl: './lich-upcoming.component.css'
})
export class LichUpcomingComponent implements OnInit {
  private lichNghiService = inject(LichNghiService);
  private toastr = inject(ToastrService);

  allDons = signal<DonYeuCauDto[]>([]);
  isLoading = signal(false);

  // Filter options
  filters = signal<FilterOptions>({
    loaiDon: 'all',
    trangThai: 'all',
    sortBy: 'ngayBatDau',
    sortOrder: 'asc'
  });

  // Computed: Filtered and sorted dons
  filteredDons = computed(() => {
    let dons = this.allDons();
    const filter = this.filters();

    // Filter by loaiDon
    if (filter.loaiDon !== 'all') {
      dons = dons.filter(d => d.loaiDon === filter.loaiDon);
    }

    // Filter by trangThai
    if (filter.trangThai !== 'all') {
      dons = dons.filter(d => d.trangThai === filter.trangThai);
    }

    // Sort
    dons = [...dons].sort((a, b) => {
      const fieldA = filter.sortBy === 'ngayBatDau' ? new Date(a.ngayBatDau!) : new Date(a.ngayTao);
      const fieldB = filter.sortBy === 'ngayBatDau' ? new Date(b.ngayBatDau!) : new Date(b.ngayTao);
      
      const comparison = fieldA.getTime() - fieldB.getTime();
      return filter.sortOrder === 'asc' ? comparison : -comparison;
    });

    return dons;
  });

  ngOnInit(): void {
    this.loadUpcomingDons();
  }

  async loadUpcomingDons(): Promise<void> {
    try {
      this.isLoading.set(true);

      const dashboard = await this.lichNghiService.getMyDashboard().toPromise();
      if (dashboard) {
        this.allDons.set(dashboard.donNghiSapToi);
      }
    } catch (error: any) {
      this.toastr.error(error.error?.message || 'Không thể tải dữ liệu', 'Lỗi');
    } finally {
      this.isLoading.set(false);
    }
  }

  updateFilter(key: keyof FilterOptions, value: any): void {
    this.filters.update(f => ({ ...f, [key]: value }));
  }

  resetFilters(): void {
    this.filters.set({
      loaiDon: 'all',
      trangThai: 'all',
      sortBy: 'ngayBatDau',
      sortOrder: 'asc'
    });
  }

  getLoaiDonBadgeClass(loaiDon: string): string {
    const badges: Record<string, string> = {
      'NghiPhep': 'bg-primary',
      'CongTac': 'bg-info',
      'LamThemGio': 'bg-warning text-dark',
      'DiMuon': 'bg-danger'
    };
    return badges[loaiDon] || 'bg-secondary';
  }

  getLoaiDonText(loaiDon: string): string {
    const texts: Record<string, string> = {
      'NghiPhep': 'Nghỉ Phép',
      'CongTac': 'Công Tác',
      'LamThemGio': 'Làm Thêm Giờ',
      'DiMuon': 'Đi Muộn'
    };
    return texts[loaiDon] || loaiDon;
  }

  getTrangThaiBadgeClass(trangThai: string): string {
    const badges: Record<string, string> = {
      'ChoDuyet': 'bg-warning text-dark',
      'DaDuyet': 'bg-success',
      'TuChoi': 'bg-danger',
      'HuyBo': 'bg-secondary'
    };
    return badges[trangThai] || 'bg-secondary';
  }

  getTrangThaiText(trangThai: string): string {
    const texts: Record<string, string> = {
      'ChoDuyet': 'Chờ Duyệt',
      'DaDuyet': 'Đã Duyệt',
      'TuChoi': 'Từ Chối',
      'HuyBo': 'Hủy Bỏ'
    };
    return texts[trangThai] || trangThai;
  }

  getLoaiNghiPhepText(loaiNghiPhep?: string): string {
    if (!loaiNghiPhep) return '';
    const texts: Record<string, string> = {
      'FullDay': 'Cả ngày',
      'Morning': 'Sáng',
      'Afternoon': 'Chiều'
    };
    return texts[loaiNghiPhep] || loaiNghiPhep;
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleDateString('vi-VN');
  }
}
