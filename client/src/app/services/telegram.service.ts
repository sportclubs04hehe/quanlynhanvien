import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, interval, switchMap, takeWhile } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { 
  TelegramLinkResponse, 
  TelegramLinkStatus, 
  TelegramTokenVerification,
  ApiResponse 
} from '../types/telegram.model';

@Injectable({
  providedIn: 'root'
})
export class TelegramService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/telegram`;

  // BehaviorSubject để theo dõi trạng thái liên kết
  private linkStatusSubject = new BehaviorSubject<TelegramLinkStatus | null>(null);
  public linkStatus$ = this.linkStatusSubject.asObservable();

  /**
   * Tạo deep link để liên kết Telegram
   * POST /api/telegram/generate-link
   */
  generateLink(): Observable<TelegramLinkResponse> {
    return this.http.post<TelegramLinkResponse>(`${this.apiUrl}/generate-link`, {});
  }

  /**
   * Kiểm tra trạng thái liên kết Telegram
   * GET /api/telegram/link-status
   */
  getLinkStatus(): Observable<TelegramLinkStatus> {
    return this.http.get<TelegramLinkStatus>(`${this.apiUrl}/link-status`).pipe(
      tap(status => this.linkStatusSubject.next(status))
    );
  }

  /**
   * Hủy liên kết Telegram
   * POST /api/telegram/unlink
   */
  unlink(): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/unlink`, {}).pipe(
      tap(() => {
        // Cập nhật trạng thái sau khi unlink
        this.getLinkStatus().subscribe();
      })
    );
  }

  /**
   * Kiểm tra token có hợp lệ không (for testing)
   * GET /api/telegram/verify-token/{token}
   */
  verifyToken(token: string): Observable<TelegramTokenVerification> {
    return this.http.get<TelegramTokenVerification>(`${this.apiUrl}/verify-token/${token}`);
  }

  /**
   * Polling để kiểm tra xem user đã link Telegram chưa
   * Gọi API mỗi 3 giây cho đến khi isLinked = true hoặc token hết hạn
   * @param maxDurationSeconds Thời gian tối đa để poll (mặc định 600s = 10 phút)
   */
  pollLinkStatus(maxDurationSeconds: number = 600): Observable<TelegramLinkStatus> {
    const startTime = Date.now();
    
    return interval(3000).pipe( // Poll mỗi 3 giây
      switchMap(() => {
        return this.getLinkStatus();
      }),
      takeWhile(status => {
        const elapsed = (Date.now() - startTime) / 1000;
        const shouldContinue = !status.isLinked && status.hasPendingToken && elapsed < maxDurationSeconds;
        
        // Dừng khi: đã link HOẶC không còn pending token HOẶC quá thời gian
        return shouldContinue;
      }, true) // true = emit giá trị cuối cùng trước khi dừng
    );
  }

  /**
   * Lấy trạng thái hiện tại từ BehaviorSubject (không gọi API)
   */
  getCurrentLinkStatus(): TelegramLinkStatus | null {
    return this.linkStatusSubject.value;
  }

  /**
   * Refresh trạng thái liên kết (gọi API và cập nhật subject)
   */
  refreshLinkStatus(): Observable<TelegramLinkStatus> {
    return this.getLinkStatus();
  }
}
