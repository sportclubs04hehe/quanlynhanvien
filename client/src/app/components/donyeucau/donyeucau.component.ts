import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { APP_ROLES } from '../../constants/roles.constants';
import { DonMyListComponent } from './don-my-list/don-my-list.component';
import { DonApproveListComponent } from './don-approve-list/don-approve-list.component';

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
    DonApproveListComponent
  ],
  templateUrl: './donyeucau.component.html',
  styleUrl: './donyeucau.component.css'
})
export class DonyeucauComponent {
  private authService = inject(AuthService);
  
  // Current active tab
  activeTab = signal<TabType>('my-dons');
  
  // Get current user
  currentUser = this.authService.currentUser;
  
  // Define all tabs
  private allTabs: Tab[] = [
    {
      id: 'approve',
      label: 'Duyệt Đơn',
      icon: 'bi-clipboard-check',
      component: DonApproveListComponent,
      roles: [APP_ROLES.TRUONG_PHONG, APP_ROLES.GIAM_DOC]
    },
    {
      id: 'my-dons',
      label: 'Đơn Của Tôi',
      icon: 'bi-file-earmark-text',
      component: DonMyListComponent
    },
    // TODO: Implement later
    // {
    //   id: 'admin',
    //   label: 'Quản Lý Tất Cả',
    //   icon: 'bi-list-check',
    //   component: DonAdminListComponent,
    //   roles: [APP_ROLES.GIAM_DOC]
    // },
    // {
    //   id: 'stats',
    //   label: 'Thống Kê',
    //   icon: 'bi-graph-up',
    //   component: DonStatsComponent,
    //   roles: [APP_ROLES.GIAM_DOC]
    // }
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
