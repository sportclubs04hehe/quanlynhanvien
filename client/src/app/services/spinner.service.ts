import { Injectable, inject } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root'
})
export class SpinnerService {
  private spinner = inject(NgxSpinnerService);

  show(message: string = 'Đang tải...') {
    this.spinner.show(undefined, {
      type: 'ball-atom',
      size: 'medium',
      bdColor: 'rgba(0, 0, 0, 0.8)',
      color: '#fff',
      fullScreen: true,
      template: `<p style="color: white">${message}</p>`
    });
  }

  hide() {
    this.spinner.hide();
  }
}
