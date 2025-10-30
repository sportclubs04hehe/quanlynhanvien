import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ThemSuaPhongbanComponent } from './them-sua-phongban/them-sua-phongban.component';
import { NoficationComponent } from '../../shared/modal/nofication/nofication.component';
import { PhongbanService } from '../../services/phongban.service';
import { SpinnerService } from '../../services/spinner.service';
import { PhongBanDto } from '../../types/phongban.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil, finalize } from 'rxjs';

@Component({
  selector: 'app-phongban',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phongban.component.html',
  styleUrl: './phongban.component.css'
})
export class PhongbanComponent implements OnInit, OnDestroy {
  private modal = inject(NgbModal);
  private phongbanService = inject(PhongbanService);
  private spinner = inject(SpinnerService);

  phongBans = signal<PhongBanDto[]>([]);
  errorMessage = signal<string | null>(null);
  
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
    this.spinner.show('Đang tải danh sách phòng ban...');

    const term = this.searchTerm().trim();
    const searchValue = term.length >= 2 ? term : undefined;

    this.phongbanService.getAll(this.pageNumber(), this.pageSize(), searchValue)
      .pipe(finalize(() => this.spinner.hide()))
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
    const modalRef = this.modal.open(ThemSuaPhongbanComponent, { 
      size: 'md',
      backdrop: 'static'
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
    const modalRef = this.modal.open(ThemSuaPhongbanComponent, { 
      size: 'md',
      backdrop: 'static'
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
    const modalRef = this.modal.open(NoficationComponent, {
      centered: true,
      backdrop: 'static'
    });

    modalRef.componentInstance.title = 'Xác nhận xóa';
    modalRef.componentInstance.message = `Bạn có chắc chắn muốn xóa phòng ban "${phongBan.tenPhongBan}"?`;
    modalRef.componentInstance.confirmText = 'Xóa';
    modalRef.componentInstance.cancelText = 'Hủy';
    modalRef.componentInstance.type = 'success';

    modalRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.spinner.show('Đang xóa phòng ban...');
          this.phongbanService.delete(phongBan.id)
            .pipe(finalize(() => this.spinner.hide()))
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

  // Expose Math to template
  Math = Math;
}

