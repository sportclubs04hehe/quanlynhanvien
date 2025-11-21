import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleService } from '../../services/role.service';
import { LichDashboardComponent } from './lich-dashboard/lich-dashboard.component';
import { LichCalendarComponent } from './lich-calendar/lich-calendar.component';
import { LichUpcomingComponent } from './lich-upcoming/lich-upcoming.component';
import { LichAdminQuotaListComponent } from './lich-admin-quota-list/lich-admin-quota-list.component';

type TabType = 'dashboard' | 'calendar' | 'upcoming' | 'admin-quota';

interface Tab {
  id: TabType;
  label: string;
  icon: string;
  requiresGiamDoc?: boolean;
}

@Component({
  selector: 'app-lich-nghi',
  standalone: true,
  imports: [
    CommonModule,
    LichDashboardComponent,
    LichCalendarComponent,
    LichUpcomingComponent,
    LichAdminQuotaListComponent
  ],
  templateUrl: './lich-nghi.component.html',
  styleUrl: './lich-nghi.component.css'
})
export class LichNghiComponent {
  roleService = inject(RoleService);
  activeTab = signal<TabType>('dashboard');

  // All available tabs
  allTabs: Tab[] = [
    { id: 'dashboard', label: 'Tổng Quan', icon: 'bi-speedometer2' },
    { id: 'calendar', label: 'Lịch Nghỉ', icon: 'bi-calendar3' },
    { id: 'upcoming', label: 'Sắp Tới', icon: 'bi-calendar-event' },
    { id: 'admin-quota', label: 'Hạn Mức Nghỉ Phép', icon: 'bi-people', requiresGiamDoc: true }
  ];

  // Computed: Filter tabs based on role
  tabs = computed(() => {
    return this.allTabs.filter(tab => {
      // Nếu tab yêu cầu Giám Đốc, check role
      if (tab.requiresGiamDoc) {
        return this.roleService.isGiamDoc();
      }
      return true;
    });
  });

  switchTab(tabId: TabType): void {
    this.activeTab.set(tabId);
  }

  isActive(tabId: TabType): boolean {
    return this.activeTab() === tabId;
  }
}
