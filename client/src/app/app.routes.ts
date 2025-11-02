import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard, loginGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';
import { PhongbanComponent } from './components/phongban/phongban.component';
import { QuanlynhanvienComponent } from './components/quanlynhanvien/quanlynhanvien.component';
import { APP_ROLES } from './constants/roles.constants';
import { ActiveSessionsComponent } from './components/account/active-sessions/active-sessions.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  { path: 'login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'auth/login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },

  // Quản lý nhân viên - Yêu cầu role Giam Doc hoặc Pho Giam Doc
  {
    path: 'quanlynhanvien',
    component: QuanlynhanvienComponent,
    canActivate: [authGuard, roleGuard],
    canDeactivate: [unsavedChangesGuard],
    data: { roles: [APP_ROLES.GIAM_DOC, APP_ROLES.PHO_GIAM_DOC] } // ← Dùng constants
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
    canDeactivate: [unsavedChangesGuard]
  },

  // Add more routes as needed
  // { path: 'auth/register', component: RegisterComponent, canActivate: [loginGuard] },
  // { path: 'auth/forgot-password', component: ForgotPasswordComponent, canActivate: [loginGuard] },
  { path: '**', redirectTo: '/dashboard' }
];
