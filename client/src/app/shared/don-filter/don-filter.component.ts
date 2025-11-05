import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { 
  FilterDonYeuCauDto, 
  LoaiDonYeuCau, 
  TrangThaiDon,
  LOAI_DON_DISPLAY_NAMES,
  TRANG_THAI_DON_DISPLAY_NAMES
} from '../../types/don.model';

@Component({
  selector: 'app-don-filter',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbDatepickerModule],
  templateUrl: './don-filter.component.html',
  styleUrl: './don-filter.component.css'
})
export class DonFilterComponent implements OnInit {
  @Output() filterChange = new EventEmitter<FilterDonYeuCauDto>();
  @Output() resetFilter = new EventEmitter<void>();
  
  // Filter model
  searchTerm: string = '';
  selectedLoaiDon: LoaiDonYeuCau | '' = '';
  selectedTrangThai: TrangThaiDon | '' = '';
  tuNgay: NgbDateStruct | null = null;
  denNgay: NgbDateStruct | null = null;
  
  // Expose enums and display names to template
  readonly loaiDonOptions = Object.entries(LOAI_DON_DISPLAY_NAMES).map(([key, value]) => ({
    value: key as LoaiDonYeuCau,
    label: value
  }));
  
  readonly trangThaiOptions = Object.entries(TRANG_THAI_DON_DISPLAY_NAMES).map(([key, value]) => ({
    value: key as TrangThaiDon,
    label: value
  }));
  
  ngOnInit(): void {
    // Initialize with default filter
    this.onFilterChange();
  }
  
  /**
   * Convert NgbDateStruct to ISO string
   */
  private dateStructToString(date: NgbDateStruct | null): string | undefined {
    if (!date) return undefined;
    return `${date.year}-${String(date.month).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`;
  }
  
  /**
   * Emit filter changes
   */
  onFilterChange(): void {
    const filter: FilterDonYeuCauDto = {
      searchTerm: this.searchTerm.trim() || undefined,
      loaiDon: this.selectedLoaiDon || undefined,
      trangThai: this.selectedTrangThai || undefined,
      tuNgay: this.dateStructToString(this.tuNgay),
      denNgay: this.dateStructToString(this.denNgay)
    };
    
    this.filterChange.emit(filter);
  }
  
  /**
   * Reset all filters
   */
  onReset(): void {
    this.searchTerm = '';
    this.selectedLoaiDon = '';
    this.selectedTrangThai = '';
    this.tuNgay = null;
    this.denNgay = null;
    
    this.resetFilter.emit();
    this.onFilterChange();
  }
  
  /**
   * Check if any filter is active
   */
  hasActiveFilters(): boolean {
    return !!(
      this.searchTerm.trim() ||
      this.selectedLoaiDon ||
      this.selectedTrangThai ||
      this.tuNgay ||
      this.denNgay
    );
  }
}
