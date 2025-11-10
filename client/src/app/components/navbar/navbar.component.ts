import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from "@angular/router";
import { AuthService } from '../../services/auth.service';
import { HasRoleDirective } from '../../directives/has-role.directive';
import { APP_ROLES } from '../../constants/roles.constants';
import { TelegramSettingsComponent } from '../account/telegram-settings/telegram-settings.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLinkActive, RouterLink, CommonModule, HasRoleDirective, TelegramSettingsComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Expose roles to template
  readonly APP_ROLES = APP_ROLES;

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
