import { Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
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
export class DonFilterComponent implements OnInit, OnDestroy {
  @Output() filterChange = new EventEmitter<FilterDonYeuCauDto>();
  @Output() resetFilter = new EventEmitter<void>();
  
  // Filter model
  searchTerm: string = '';
  selectedLoaiDon: LoaiDonYeuCau | '' = '';
  selectedTrangThai: TrangThaiDon | '' = '';
  tuNgay: NgbDateStruct | null = null;
  denNgay: NgbDateStruct | null = null;
  
  // Subject for search debounce
  private searchSubject$ = new Subject<string>();
  private destroy$ = new Subject<void>();
  
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
    // Setup search debounce - 500ms delay
    this.searchSubject$
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.emitFilterChange();
      });
    
    // Initialize with default filter (immediate)
    this.emitFilterChange();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Called when search input changes
   */
  onSearchChange(): void {
    this.searchSubject$.next(this.searchTerm);
  }
  
  /**
   * Convert NgbDateStruct to ISO string
   */
  private dateStructToString(date: NgbDateStruct | null): string | undefined {
    if (!date) return undefined;
    return `${date.year}-${String(date.month).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`;
  }
  
  /**
   * Emit filter changes (private helper)
   */
  private emitFilterChange(): void {
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
   * Called when dropdown/date filters change (immediate)
   */
  onFilterChange(): void {
    this.emitFilterChange();
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
    this.emitFilterChange();
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
