import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LichNghiService } from '../../../services/lich-nghi.service';
import { LichNghiCalendarDto, NgayNghiDetailDto } from '../../../types/lichnghi.model';
import { ToastrService } from 'ngx-toastr';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  isWeekend: boolean;
  ngayNghi?: NgayNghiDetailDto[];
}

@Component({
  selector: 'app-lich-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lich-calendar.component.html',
  styleUrl: './lich-calendar.component.css'
})
export class LichCalendarComponent implements OnInit {
  private lichNghiService = inject(LichNghiService);
  private toastr = inject(ToastrService);

  calendar = signal<LichNghiCalendarDto | null>(null);
  isLoading = signal(true);

  // Current viewing month/year
  currentYear = signal(new Date().getFullYear());
  currentMonth = signal(new Date().getMonth() + 1); // 1-12

  // Calendar grid
  calendarDays = computed(() => this.generateCalendarDays());

  weekDays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];

  ngOnInit(): void {
    this.loadCalendar();
  }

  loadCalendar(): void {
    this.isLoading.set(true);
    
    this.lichNghiService.getCalendar(this.currentYear(), this.currentMonth()).subscribe({
      next: (data) => {
        this.calendar.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Lỗi khi tải lịch:', err);
        this.toastr.error('Không thể tải dữ liệu lịch', 'Lỗi');
        this.isLoading.set(false);
      }
    });
  }

  generateCalendarDays(): CalendarDay[] {
    const year = this.currentYear();
    const month = this.currentMonth();
    const days: CalendarDay[] = [];

    // First day of month
    const firstDay = new Date(year, month - 1, 1);
    const lastDay = new Date(year, month, 0);

    // Start from Sunday of the week containing first day
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - startDate.getDay());

    // Generate 6 weeks (42 days) to ensure consistent grid
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let i = 0; i < 42; i++) {
      const date = new Date(startDate);
      date.setDate(startDate.getDate() + i);

      const isCurrentMonth = date.getMonth() === month - 1;
      const isToday = date.getTime() === today.getTime();
      const isWeekend = date.getDay() === 0 || date.getDay() === 6;

      // Find ngay nghi for this date
      const ngayNghi = this.calendar()?.ngayDaNghi.filter(n => {
        const nghiDate = new Date(n.ngay);
        nghiDate.setHours(0, 0, 0, 0);
        return nghiDate.getTime() === date.getTime();
      }) || [];

      days.push({
        date,
        isCurrentMonth,
        isToday,
        isWeekend,
        ngayNghi: ngayNghi.length > 0 ? ngayNghi : undefined
      });
    }

    return days;
  }

  previousMonth(): void {
    if (this.currentMonth() === 1) {
      this.currentMonth.set(12);
      this.currentYear.set(this.currentYear() - 1);
    } else {
      this.currentMonth.set(this.currentMonth() - 1);
    }
    this.loadCalendar();
  }

  nextMonth(): void {
    if (this.currentMonth() === 12) {
      this.currentMonth.set(1);
      this.currentYear.set(this.currentYear() + 1);
    } else {
      this.currentMonth.set(this.currentMonth() + 1);
    }
    this.loadCalendar();
  }

  goToToday(): void {
    const today = new Date();
    this.currentYear.set(today.getFullYear());
    this.currentMonth.set(today.getMonth() + 1);
    this.loadCalendar();
  }

  getMonthName(): string {
    const months = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
                    'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];
    return months[this.currentMonth() - 1];
  }

  getTotalNgayNghiInfo(): string {
    const cal = this.calendar();
    if (!cal) return '';
    return `Đã nghỉ: ${cal.soNgayNghiTrongThang} ngày | Làm thêm: ${cal.soGioLamThemTrongThang} giờ`;
  }

  getDayClass(day: CalendarDay): string {
    const classes = ['calendar-day'];
    
    if (!day.isCurrentMonth) classes.push('other-month');
    if (day.isToday) classes.push('today');
    if (day.isWeekend) classes.push('weekend');
    if (day.ngayNghi && day.ngayNghi.length > 0) classes.push('has-leave');
    
    return classes.join(' ');
  }

  getLeaveTypeClass(loaiNghiPhep?: string): string {
    if (!loaiNghiPhep) return 'leave-full';
    
    if (loaiNghiPhep.includes('Sáng')) return 'leave-morning';
    if (loaiNghiPhep.includes('Chiều')) return 'leave-afternoon';
    return 'leave-full';
  }
}
