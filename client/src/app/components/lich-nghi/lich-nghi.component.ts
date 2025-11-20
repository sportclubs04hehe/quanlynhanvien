import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LichDashboardComponent } from './lich-dashboard/lich-dashboard.component';
import { LichCalendarComponent } from './lich-calendar/lich-calendar.component';
import { LichUpcomingComponent } from './lich-upcoming/lich-upcoming.component';

type TabType = 'dashboard' | 'calendar' | 'upcoming';

interface Tab {
  id: TabType;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-lich-nghi',
  standalone: true,
  imports: [
    CommonModule,
    LichDashboardComponent,
    LichCalendarComponent,
    LichUpcomingComponent
  ],
  templateUrl: './lich-nghi.component.html',
  styleUrl: './lich-nghi.component.css'
})
export class LichNghiComponent {
  activeTab = signal<TabType>('dashboard');

  tabs: Tab[] = [
    { id: 'dashboard', label: 'Tổng Quan', icon: 'bi-speedometer2' },
    { id: 'calendar', label: 'Lịch Nghỉ', icon: 'bi-calendar3' },
    { id: 'upcoming', label: 'Sắp Tới', icon: 'bi-calendar-event' }
  ];

  switchTab(tabId: TabType): void {
    this.activeTab.set(tabId);
  }

  isActive(tabId: TabType): boolean {
    return this.activeTab() === tabId;
  }
}
