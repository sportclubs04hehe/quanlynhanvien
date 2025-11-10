import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { TelegramService } from '../../../services/telegram.service';
import { TelegramLinkStatus, TelegramLinkResponse } from '../../../types/telegram.model';
import { Subscription, interval } from 'rxjs';
import * as QRCode from 'qrcode';

@Component({
  selector: 'app-telegram-settings',
  standalone: true,
  imports: [], // Không cần CommonModule với Angular 18+ control flow
  templateUrl: './telegram-settings.component.html',
  styleUrl: './telegram-settings.component.css'
})
export class TelegramSettingsComponent implements OnInit, OnDestroy {
  private telegramService = inject(TelegramService);
  
  // State
  linkStatus: TelegramLinkStatus | null = null;
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;
  
  // Deep Link
  generatedLink: TelegramLinkResponse | null = null;
  showDeepLink = false;
  qrCodeDataUrl: string | null = null; // QR Code image
  activeTab: 'qr' | 'link' = 'qr'; // Tab hiện tại
  
  // Countdown timer
  countdownSeconds = 0;
  private countdownSubscription?: Subscription;
  private pollSubscription?: Subscription;

  ngOnInit(): void {
    this.loadLinkStatus();
  }

  ngOnDestroy(): void {
    this.stopCountdown();
    this.stopPolling();
  }

  /**
   * Load trạng thái liên kết từ API
   */
  loadLinkStatus(): void {
    this.loading = true;
    this.error = null;

    this.telegramService.getLinkStatus().subscribe({
      next: (status) => {
        this.linkStatus = status;
        this.loading = false;
        
        // ✅ Nếu đã liên kết rồi, DỪNG mọi polling/countdown
        if (status.isLinked) {
          this.stopCountdown();
          this.stopPolling();
          this.showDeepLink = false;
          this.generatedLink = null;
          return; // Dừng ngay, không xử lý gì thêm
        }
        
        // Nếu có pending token, bắt đầu countdown
        if (status.hasPendingToken && status.pendingTokenExpiresInSeconds > 0) {
          this.countdownSeconds = status.pendingTokenExpiresInSeconds;
          this.startCountdown();
        }
      },
      error: (err) => {
        this.error = 'Không thể tải trạng thái liên kết';
        this.loading = false;
        console.error('Error loading link status:', err);
      }
    });
  }

  /**
   * Tạo deep link mới
   */
  async generateDeepLink(): Promise<void> {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    this.telegramService.generateLink().subscribe({
      next: async (response) => {
        this.generatedLink = response;
        this.showDeepLink = true;
        this.countdownSeconds = response.expiresInSeconds;
        this.loading = false;
        this.activeTab = 'qr';

        try {
          this.qrCodeDataUrl = await QRCode.toDataURL(response.deepLink, {
            width: 300,
            margin: 2,
            color: {
              dark: '#000000',
              light: '#FFFFFF'
            },
            errorCorrectionLevel: 'M'
          });
          console.log('✅ QR Code generated successfully');
        } catch (err) {
          console.error('❌ QR Code generation failed:', err);
          this.error = 'Không thể tạo QR code';
        }
        
        // Bắt đầu countdown và polling
        this.startCountdown();
        this.startPolling();
        
        // ⚠️ KHÔNG GỌI loadLinkStatus() ở đây vì sẽ conflict với polling
        // Polling sẽ tự động update linkStatus
      },
      error: (err) => {
        if (err.error?.message) {
          this.error = err.error.message;
        } else {
          this.error = 'Không thể tạo link liên kết';
        }
        this.loading = false;
        console.error('Error generating link:', err);
      }
    });
  }

  /**
   * Mở deep link (chuyển đến Telegram)
   */
  openDeepLink(): void {
    if (this.generatedLink) {
      window.open(this.generatedLink.deepLink, '_blank');
    }
  }

  /**
   * Copy deep link vào clipboard
   */
  copyDeepLink(): void {
    if (this.generatedLink) {
      navigator.clipboard.writeText(this.generatedLink.deepLink).then(() => {
        this.successMessage = 'Đã copy link vào clipboard!';
        setTimeout(() => this.successMessage = null, 3000);
      }).catch(err => {
        console.error('Copy failed:', err);
        this.error = 'Không thể copy link';
      });
    }
  }

  /**
   * Handle click event trên input để select text
   */
  selectInputText(event: Event): void {
    const input = event.target as HTMLInputElement;
    input.select();
  }

  /**
   * Hủy liên kết Telegram
   */
  unlinkTelegram(): void {
    if (!confirm('Bạn có chắc muốn hủy liên kết Telegram?')) {
      return;
    }

    this.loading = true;
    this.error = null;

    this.telegramService.unlink().subscribe({
      next: (response) => {
        this.successMessage = response.message || 'Đã hủy liên kết thành công';
        this.loading = false;
        this.showDeepLink = false;
        this.generatedLink = null;
        this.stopCountdown();
        this.stopPolling();
        
        // Refresh trạng thái
        setTimeout(() => this.loadLinkStatus(), 500);
      },
      error: (err) => {
        this.error = err.error?.message || 'Không thể hủy liên kết';
        this.loading = false;
        console.error('Error unlinking:', err);
      }
    });
  }

  /**
   * Bắt đầu countdown timer
   */
  private startCountdown(): void {
    this.stopCountdown(); // Clear countdown cũ nếu có
    
    this.countdownSubscription = interval(1000).subscribe(() => {
      this.countdownSeconds--;
      
      if (this.countdownSeconds <= 0) {
        this.stopCountdown();
        this.showDeepLink = false;
        this.generatedLink = null;
        this.stopPolling();
        this.error = 'Link đã hết hạn. Vui lòng tạo link mới.';
      }
    });
  }

  /**
   * Dừng countdown timer
   */
  private stopCountdown(): void {
    if (this.countdownSubscription) {
      this.countdownSubscription.unsubscribe();
      this.countdownSubscription = undefined;
    }
  }

  /**
   * Bắt đầu polling để kiểm tra user đã link chưa
   */
  private startPolling(): void {
    this.stopPolling();
    
    this.pollSubscription = this.telegramService.pollLinkStatus().subscribe({
      next: (status) => {
        this.linkStatus = status;
        
        // Nếu đã link thành công
        if (status.isLinked) {
          this.successMessage = '✅ Đã liên kết Telegram thành công!';
          this.showDeepLink = false;
          this.generatedLink = null;
          this.stopCountdown();
          this.stopPolling(); // ⚠️ QUAN TRỌNG: Dừng polling ngay
        }
      },
      error: (err) => {
        console.error('❌ Polling error:', err);
        this.stopPolling(); // Dừng khi có lỗi
      },
      complete: () => {
        this.stopPolling();
        this.loadLinkStatus();
      }
    });
  }

  /**
   * Dừng polling
   */
  private stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
      this.pollSubscription = undefined;
    }
  }

  /**
   * Format countdown thành MM:SS
   */
  getFormattedCountdown(): string {
    const minutes = Math.floor(this.countdownSeconds / 60);
    const seconds = this.countdownSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  /**
   * Đóng deep link modal
   */
  closeDeepLink(): void {
    this.showDeepLink = false;
    this.qrCodeDataUrl = null; // Clear QR code
    this.stopCountdown();
    this.stopPolling();
  }

  /**
   * Chuyển tab giữa QR code và Link
   */
  switchTab(tab: 'qr' | 'link'): void {
    this.activeTab = tab;
  }
}
