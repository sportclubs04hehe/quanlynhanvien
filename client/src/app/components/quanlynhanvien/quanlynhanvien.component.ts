import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { debounceTime, distinctUntilChanged, Subject, takeUntil, finalize } from 'rxjs';
import { QuanlynhanvienService } from '../../services/quanlynhanvien.service';
import { SpinnerService } from '../../services/spinner.service';
import { NoficationComponent } from '../../shared/modal/nofication/nofication.component';
import { ThemSuaNhanvienComponent } from './them-sua-nhanvien/them-sua-nhanvien.component';
import { ChitietNhanvienComponent } from './chitiet-nhanvien/chitiet-nhanvien.component';
import { UserDto, NhanVienStatus } from '../../types/users.model';
import { CanComponentDeactivate } from '../../guards/unsaved-changes.guard';

@Component({
  selector: 'app-quanlynhanvien',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quanlynhanvien.component.html',
  styleUrl: './quanlynhanvien.component.css'
})
export class QuanlynhanvienComponent implements OnInit, OnDestroy, CanComponentDeactivate {
  private modal = inject(NgbModal);
  private nhanVienService = inject(QuanlynhanvienService);
  private spinner = inject(SpinnerService);

  users = signal<UserDto[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  
  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(0);
  
  searchTerm = signal('');
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();
  
  ngOnInit() {
    this.loadUsers();
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
      this.loadUsers();
    });
  }

  loadUsers() {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.spinner.show('Đang tải danh sách nhân viên...');

    const term = this.searchTerm().trim();
    const searchValue = term.length >= 2 ? term : undefined;

    this.nhanVienService.getAll(this.pageNumber(), this.pageSize(), searchValue)
      .pipe(finalize(() => {
        this.spinner.hide();
        this.isLoading.set(false);
      }))
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages || Math.ceil(result.totalCount / result.pageSize));
        },
        error: (error) => {
          this.errorMessage.set('Không thể tải danh sách nhân viên');
          console.error('Error loading users:', error);
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
    this.loadUsers();
  }

  openCreateModal() {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(ThemSuaNhanvienComponent, { 
      size: 'lg',
      backdrop: 'static',
      keyboard: false // Disable ESC key
    });
    
    modalRef.componentInstance.mode = 'create';
    
    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadUsers();
        }
      },
      () => {}
    );
  }

  openEditModal(user: UserDto) {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(ThemSuaNhanvienComponent, { 
      size: 'lg',
      backdrop: 'static',
      keyboard: false // Disable ESC key
    });
    
    modalRef.componentInstance.mode = 'edit';
    modalRef.componentInstance.userId = user.id;
    modalRef.componentInstance.userData = user;
    
    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadUsers();
        }
      },
      () => {}
    );
  }

  viewDetail(user: UserDto) {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(ChitietNhanvienComponent, { 
      size: 'lg'
    });
    
    modalRef.componentInstance.userId = user.id;
  }

  deleteUser(user: UserDto) {
    // Blur active element để tránh aria-hidden warning
    this.blurActiveElement();
    
    const modalRef = this.modal.open(NoficationComponent, {
      centered: true,
      backdrop: 'static'
    });

    modalRef.componentInstance.title = 'Xác nhận xóa';
    modalRef.componentInstance.message = `Bạn có chắc chắn muốn xóa nhân viên "${user.tenDayDu}"? Hành động này không thể hoàn tác.`;
    modalRef.componentInstance.confirmText = 'Xóa';
    modalRef.componentInstance.cancelText = 'Hủy';
    modalRef.componentInstance.type = 'danger';

    modalRef.result.then(
      (confirmed) => {
        if (confirmed) {
          this.spinner.show('Đang xóa nhân viên...');
          this.nhanVienService.delete(user.id)
            .pipe(finalize(() => this.spinner.hide()))
            .subscribe({
              next: () => {
                this.loadUsers();
              },
              error: (error) => {
                const errorModalRef = this.modal.open(NoficationComponent, { centered: true });
                errorModalRef.componentInstance.title = 'Lỗi';
                errorModalRef.componentInstance.message = 'Không thể xóa nhân viên. Vui lòng thử lại.';
                errorModalRef.componentInstance.type = 'danger';
                errorModalRef.componentInstance.confirmText = 'Đóng';
                errorModalRef.componentInstance.cancelText = '';
                console.error('Error deleting user:', error);
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

  getStatusLabel(status: NhanVienStatus): string {
    switch (status) {
      case NhanVienStatus.Active:
        return 'Hoạt động';
      case NhanVienStatus.Inactive:
        return 'Ngừng hoạt động';
      case NhanVienStatus.OnLeave:
        return 'Nghỉ phép';
      default:
        return 'Không xác định';
    }
  }

  getStatusClass(status: NhanVienStatus): string {
    switch (status) {
      case NhanVienStatus.Active:
        return 'bg-success';
      case NhanVienStatus.Inactive:
        return 'bg-danger';
      case NhanVienStatus.OnLeave:
        return 'bg-warning text-dark';
      default:
        return 'bg-secondary';
    }
  }

  Math = Math;
}
