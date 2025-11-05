import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TrangThaiDon, getTrangThaiDonDisplayName } from '../../types/don.model';

@Component({
  selector: 'app-don-status-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './don-status-badge.component.html',
  styleUrl: './don-status-badge.component.css'
})
export class DonStatusBadgeComponent {
  @Input({ required: true }) trangThai!: TrangThaiDon;
  
  // Expose enum to template
  readonly TrangThaiDon = TrangThaiDon;
  
  /**
   * Get display name for status
   */
  getDisplayName(): string {
    return getTrangThaiDonDisplayName(this.trangThai);
  }
  
  /**
   * Get Bootstrap badge class based on status
   */
  getBadgeClass(): string {
    switch (this.trangThai) {
      case TrangThaiDon.DangChoDuyet:
        return 'bg-warning text-dark';
      case TrangThaiDon.DaChapThuan:
        return 'bg-success';
      case TrangThaiDon.BiTuChoi:
        return 'bg-danger';
      case TrangThaiDon.DaHuy:
        return 'bg-secondary';
      default:
        return 'bg-secondary';
    }
  }
  
  /**
   * Get Bootstrap icon for status
   */
  getIconClass(): string {
    switch (this.trangThai) {
      case TrangThaiDon.DangChoDuyet:
        return 'bi-clock-history';
      case TrangThaiDon.DaChapThuan:
        return 'bi-check-circle-fill';
      case TrangThaiDon.BiTuChoi:
        return 'bi-x-circle-fill';
      case TrangThaiDon.DaHuy:
        return 'bi-dash-circle-fill';
      default:
        return 'bi-question-circle';
    }
  }
}
