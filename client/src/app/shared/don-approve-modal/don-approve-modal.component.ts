import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { DonYeuCauDto, TrangThaiDon } from '../../types/don.model';

export interface ApprovalResult {
  trangThai: TrangThaiDon;
  ghiChuNguoiDuyet?: string;
}

@Component({
  selector: 'app-don-approve-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './don-approve-modal.component.html',
  styleUrl: './don-approve-modal.component.css'
})
export class DonApproveModalComponent {
  @Input({ required: true }) don!: DonYeuCauDto;
  
  ghiChu: string = '';
  readonly maxLength = 500;
  
  constructor(public activeModal: NgbActiveModal) {}
  
  /**
   * Get remaining characters
   */
  getRemainingChars(): number {
    return this.maxLength - this.ghiChu.length;
  }
  
  /**
   * Check if note is too long
   */
  isNoteTooLong(): boolean {
    return this.ghiChu.length > this.maxLength;
  }
  
  /**
   * Approve the request
   */
  onApprove(): void {
    if (this.isNoteTooLong()) return;
    
    const result: ApprovalResult = {
      trangThai: TrangThaiDon.DaChapThuan,
      ghiChuNguoiDuyet: this.ghiChu.trim() || undefined
    };
    
    this.activeModal.close(result);
  }
  
  /**
   * Reject the request
   */
  onReject(): void {
    if (this.isNoteTooLong()) return;
    
    const result: ApprovalResult = {
      trangThai: TrangThaiDon.BiTuChoi,
      ghiChuNguoiDuyet: this.ghiChu.trim() || undefined
    };
    
    this.activeModal.close(result);
  }
  
  /**
   * Cancel and close modal
   */
  onCancel(): void {
    this.activeModal.dismiss('cancel');
  }
}
