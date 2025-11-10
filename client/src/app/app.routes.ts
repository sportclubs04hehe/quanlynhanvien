import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard, loginGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { PhongbanComponent } from './components/phongban/phongban.component';
import { QuanlynhanvienComponent } from './components/quanlynhanvien/quanlynhanvien.component';
import { DonyeucauComponent } from './components/donyeucau/donyeucau.component';
import { APP_ROLES } from './constants/roles.constants';
import { ActiveSessionsComponent } from './components/account/active-sessions/active-sessions.component';
import { TelegramSettingsComponent } from './components/account/telegram-settings/telegram-settings.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/don-yeu-cau',
    pathMatch: 'full'
  },
  { path: 'login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'auth/login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'don-yeu-cau', component: DonyeucauComponent, canActivate: [authGuard] },

  // Quản lý nhân viên - Yêu cầu role Giam Doc hoặc Truong Phong
  {
    path: 'quanlynhanvien',
    component: QuanlynhanvienComponent,
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: [APP_ROLES.GIAM_DOC, APP_ROLES.TRUONG_PHONG] } // ← Dùng constants
  },
  {
    path: 'sessions',
    component: ActiveSessionsComponent,
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
  },

  // Phòng ban - Tất cả user đã login đều xem được
  {
    path: 'phongban',
    component: PhongbanComponent,
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: [APP_ROLES.GIAM_DOC, APP_ROLES.TRUONG_PHONG] }
  },

  // Đơn yêu cầu - Tất cả user đã login đều có thể truy cập
  {
    path: 'don-yeu-cau',
    component: DonyeucauComponent,
    canActivate: [authGuard]
  },

  {
    path: 'telegram',
    component: TelegramSettingsComponent,
    canActivate: [authGuard]
  },

  // Add more routes as needed
  // { path: 'auth/register', component: RegisterComponent, canActivate: [loginGuard] },
  // { path: 'auth/forgot-password', component: ForgotPasswordComponent, canActivate: [loginGuard] },
  { path: '**', redirectTo: '/don-yeu-cau' }
];
