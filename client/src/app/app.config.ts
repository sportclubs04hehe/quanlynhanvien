import { ApplicationConfig, provideZoneChangeDetection, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { loadingInterceptor } from './interceptors/loading.interceptor';
import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { provideToastr } from 'ngx-toastr';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { NgbDateParserFormatter } from '@ng-bootstrap/ng-bootstrap';
import { CustomDateParserFormatter } from './shared/datepicker-i18n.config';

// Đăng ký locale data cho tiếng Việt
registerLocaleData(localeVi);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([
        loadingInterceptor,  // Loading spinner
        authInterceptor,     // Auth token
        errorInterceptor     // Error handling
      ]) 
    ),
    provideAnimations(),
    provideToastr(),
    provideCharts(withDefaultRegisterables()),
    // Cấu hình datepicker format dd/MM/yyyy
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter }
  ]
};
