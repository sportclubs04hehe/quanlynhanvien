import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { APP_ROLES } from '../../constants/roles.constants';
import { DonMyListComponent } from './don-my-list/don-my-list.component';
import { DonApproveListComponent } from './don-approve-list/don-approve-list.component';
import { DonAdminListComponent } from './don-admin-list/don-admin-list.component';
import { DonStatsComponent } from './don-stats/don-stats.component';

type TabType = 'my-dons' | 'approve' | 'admin' | 'stats';

interface Tab {
  id: TabType;
  label: string;
  icon: string;
  component: any;
  roles?: string[]; // Nếu không có roles, hiển thị cho tất cả
}

@Component({
  selector: 'app-donyeucau',
  standalone: true,
  imports: [
    CommonModule, 
    DonMyListComponent,
    DonApproveListComponent,
    DonAdminListComponent,
    DonStatsComponent
  ],
  templateUrl: './donyeucau.component.html',
  styleUrl: './donyeucau.component.css'
})
export class DonyeucauComponent implements OnInit {
  private authService = inject(AuthService);
  
  // Current active tab (will be set in ngOnInit based on user role)
  activeTab = signal<TabType>('my-dons');
  
  // Get current user
  currentUser = this.authService.currentUser;
  
  ngOnInit(): void {
    // Set default tab: Thống Kê luôn là tab đầu tiên
    this.activeTab.set('stats');
  }
  
  // Define all tabs - Thứ tự: Thống Kê → Duyệt Đơn → Quản Lý Đã Xử Lý → Đơn Của Tôi
  private allTabs: Tab[] = [
    {
      id: 'stats',
      label: 'Thống Kê',
      icon: 'bi-bar-chart-fill',
      component: DonStatsComponent
      // Không có roles = tất cả user đều xem được stats của mình
      // Stats component tự handle logic: Giám Đốc thấy toàn công ty, Nhân viên thấy của mình
    },
    {
      id: 'approve',
      label: 'Duyệt Đơn',
      icon: 'bi-clipboard-check',
      component: DonApproveListComponent,
      roles: [APP_ROLES.TRUONG_PHONG, APP_ROLES.GIAM_DOC]
    },
    {
      id: 'admin',
      label: 'Quản Lý Đơn Đã Xử Lý',
      icon: 'bi-shield-lock',
      component: DonAdminListComponent,
      roles: [APP_ROLES.GIAM_DOC]
    },
    {
      id: 'my-dons',
      label: 'Đơn Của Tôi',
      icon: 'bi-file-earmark-text',
      component: DonMyListComponent
    }
  ];
  
  // Computed visible tabs based on user role
  visibleTabs = computed(() => {
    const user = this.currentUser();
    if (!user) return [];
    
    return this.allTabs.filter(tab => {
      // If no roles required, show to everyone
      if (!tab.roles || tab.roles.length === 0) return true;
      
      // Check if user has any of the required roles
      return user.roles.some(userRole => tab.roles!.includes(userRole));
    });
  });
  
  // Get active component
  activeComponent = computed(() => {
    const tab = this.allTabs.find(t => t.id === this.activeTab());
    return tab?.component;
  });
  
  /**
   * Switch to a tab
   */
  switchTab(tabId: TabType): void {
    this.activeTab.set(tabId);
  }
  
  /**
   * Check if tab is active
   */
  isActive(tabId: TabType): boolean {
    return this.activeTab() === tabId;
  }
}
