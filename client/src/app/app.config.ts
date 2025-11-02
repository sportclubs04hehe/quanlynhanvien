import { ApplicationConfig, provideZoneChangeDetection, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';

// Đăng ký locale data cho tiếng Việt
registerLocaleData(localeVi);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, errorInterceptor]) 
    ),
    provideAnimations(),
    // { provide: LOCALE_ID, useValue: 'vi' } 
  ]
};
