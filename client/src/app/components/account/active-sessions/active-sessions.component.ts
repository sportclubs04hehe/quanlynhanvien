import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { LocalDatePipe } from '../../../shared/pipes/local-date.pipe';
import { finalize } from 'rxjs';
import { Session } from '../../../types/session.model';

@Component({
  selector: 'app-active-sessions',
  standalone: true,
  imports: [CommonModule, LocalDatePipe],
  templateUrl: './active-sessions.component.html',
  styleUrl: './active-sessions.component.css'
})
export class ActiveSessionsComponent implements OnInit {
  private authService = inject(AuthService);

  sessions = signal<Session[]>([]);

  ngOnInit() {
    this.loadSessions();
  }

  loadSessions() {
    this.authService.getActiveSessions()
      .subscribe({
        next: (sessions) => {
          // Parse ISO string sang Date object để Angular tự động convert timezone
          const parsedSessions = sessions.map(s => ({
            ...s,
            createdAt: new Date(s.createdAt),
            expiresAt: new Date(s.expiresAt)
          }));
          this.sessions.set(parsedSessions);
        },
        error: (err) => {
          console.error('Error loading sessions:', err);
          alert('Không thể tải danh sách sessions');
        }
      });
  }

  getDeviceInfo(session: Session): string {
    // Có thể parse token hoặc lưu thêm device info
    return `Session ${session.token}`;
  }

  revokeSession(session: Session) {
    if (!confirm('Bạn có chắc chắn muốn đăng xuất thiết bị này?')) {
      return;
    }

    this.authService.revokeSession(session.id)
      .subscribe({
        next: () => {
          alert('Đã đăng xuất thiết bị thành công');
          // Reload danh sách sessions
          this.loadSessions();
        },
        error: (err) => {
          console.error('Error revoking session:', err);
          alert('Không thể đăng xuất thiết bị này');
        }
      });
  }

  revokeAllSessions() {
    if (!confirm('Đăng xuất tất cả thiết bị? Bạn sẽ phải đăng nhập lại.')) {
      return;
    }

    this.authService.revokeAllTokens()
      .subscribe({
        next: () => {
          alert('Đã đăng xuất tất cả thiết bị. Vui lòng đăng nhập lại.');
        },
        error: (err) => {
          console.error('Error revoking all tokens:', err);
          alert('Có lỗi xảy ra');
        }
      });
  }
}


