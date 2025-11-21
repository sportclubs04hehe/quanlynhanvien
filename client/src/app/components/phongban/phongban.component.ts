import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ThemSuaPhongbanComponent } from './them-sua-phongban/them-sua-phongban.component';
import { NoficationComponent } from '../../shared/modal/nofication/nofication.component';
import { PhongbanService } from '../../services/phongban.service';
import { PhongBanDto } from '../../types/phongban.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil, finalize } from 'rxjs';
import { CanComponentDeactivate } from '../../guards/unsaved-changes.guard';

@Component({
  selector: 'app-phongban',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phongban.component.html',
  styleUrl: './phongban.component.css'
})
export class PhongbanComponent implements OnInit, OnDestroy, CanComponentDeactivate {
  private modal = inject(NgbModal);
  private phongbanService = inject(PhongbanService);

  phongBans = signal<PhongBanDto[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  
  // Pagination
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  // Search with debounce
  searchTerm = signal('');
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();
  
  ngOnInit() {
    this.loadPhongBans();
    this.setupSearchDebounce();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // CanComponentDeactivate implementation
  canDeactivate(): boolean {
    // Kiểm tra xem có modal nào đang mở không
    return !this.modal.hasOpenModals();
  }

  private setupSearchDebounce() {
    this.searchSubject.pipe(
      debounceTime(500), 
      distinctUntilChanged(), 
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm.set(searchTerm);
      this.pageNumber.set(1);
      this.loadPhongBans();
    });
  }

  loadPhongBans() {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    const term = this.searchTerm().trim();
    const searchValue = term.length >= 2 ? term : undefined;

    this.phongbanService.getAll(this.pageNumber(), this.pageSize(), searchValue)
      .pipe(finalize(() => {
        this.isLoading.set(false);
      }))
      .subscribe({
        next: (result) => {
          this.phongBans.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages || Math.ceil(result.totalCount / result.pageSize));
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải danh sách phòng ban');
          console.error('Error loading phong bans:', error);
        }
      });
  }

  onSearchInput(term: string) {
    this.searchSubject.next(term);
  }

  clearSearch() {
    this.searchTerm.set('');
    this.searchSubject.next('');
  }

  onPageChange(page: number) {
    this.pageNumber.set(page);
    this.loadPhongBans();
  }

  openCreateModal() {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(ThemSuaPhongbanComponent, { 
      size: 'md',
      backdrop: 'static',
      keyboard: false // Disable ESC key
    });
    
    modalRef.componentInstance.mode = 'create';
    
    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadPhongBans();
        }
      },
      () => {} // Dismissed
    );
  }

  openEditModal(phongBan: PhongBanDto) {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(ThemSuaPhongbanComponent, { 
      size: 'md',
      backdrop: 'static',
      keyboard: false // Disable ESC key
    });
    
    modalRef.componentInstance.mode = 'edit';
    modalRef.componentInstance.phongBanId = phongBan.id;
    modalRef.componentInstance.phongBanData = phongBan;
    
    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadPhongBans();
        }
      },
      () => {}
    );
  }

  deletePhongBan(phongBan: PhongBanDto) {
    this.blurActiveElement();
    
    const modalRef = this.modal.open(NoficationComponent, {
      centered: true,
      backdrop: 'static'
    });

    modalRef.componentInstance.title = 'Xác nhận xóa';
    modalRef.componentInstance.message = `Bạn có chắc chắn muốn xóa phòng ban "${phongBan.tenPhongBan}"? Hành động này không thể hoàn tác.`;
    modalRef.componentInstance.confirmText = 'Xóa';
    modalRef.componentInstance.cancelText = 'Hủy';
    modalRef.componentInstance.type = 'danger';

    modalRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.phongbanService.delete(phongBan.id)
            .subscribe({
              next: () => {
                this.loadPhongBans();
              },
              error: (error) => {
                const errorModalRef = this.modal.open(NoficationComponent, { centered: true });
                errorModalRef.componentInstance.title = 'Lỗi';
                errorModalRef.componentInstance.message = 'Không thể xóa phòng ban. Có thể phòng ban này đang có nhân viên.';
                errorModalRef.componentInstance.type = 'danger';
                errorModalRef.componentInstance.confirmText = 'Đóng';
                errorModalRef.componentInstance.cancelText = '';
                console.error('Error deleting phong ban:', error);
              }
            });
        }
      },
      () => {}
    );
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages() }, (_, i) => i + 1);
  }

  private blurActiveElement(): void {
    const activeElement = document.activeElement as HTMLElement;
    if (activeElement && typeof activeElement.blur === 'function') {
      activeElement.blur();
    }
  }

  // Expose Math to template
  Math = Math;
}

