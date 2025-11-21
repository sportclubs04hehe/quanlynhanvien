import { Component, signal, inject, computed, OnInit } from '@angular/core';
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
export class LichNghiComponent implements OnInit {
  roleService = inject(RoleService);
  activeTab = signal<TabType>('dashboard');

  // All available tabs
  allTabs: Tab[] = [
    { id: 'dashboard', label: 'Tổng Quan', icon: 'bi-speedometer2' },
    { id: 'calendar', label: 'Lịch Nghỉ', icon: 'bi-calendar3-fill' },
    { id: 'upcoming', label: 'Sắp Tới', icon: 'bi-calendar-event-fill' },
    { id: 'admin-quota', label: 'Quản Lý Hạn Mức Nghỉ Phép', icon: 'bi-people-fill', requiresGiamDoc: true }
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

  ngOnInit(): void {
    // Nếu là Giám Đốc, mặc định vào tab "Hạn Mức Nghỉ Phép"
    if (this.roleService.isGiamDoc()) {
      this.activeTab.set('admin-quota');
    }
  }

  switchTab(tabId: TabType): void {
    this.activeTab.set(tabId);
  }

  isActive(tabId: TabType): boolean {
    return this.activeTab() === tabId;
  }
}
