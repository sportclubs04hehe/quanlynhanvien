import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard, loginGuard } from './guards/auth.guard';

export const routes: Routes = [
  { 
    path: '', 
    redirectTo: '/dashboard', 
    pathMatch: 'full' 
  },
  { path: 'login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'auth/login', component: LoginComponent, canActivate: [loginGuard] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  // Add more routes as needed
  // { path: 'auth/register', component: RegisterComponent, canActivate: [loginGuard] },
  // { path: 'auth/forgot-password', component: ForgotPasswordComponent, canActivate: [loginGuard] },
  { path: '**', redirectTo: '/dashboard' }
];
