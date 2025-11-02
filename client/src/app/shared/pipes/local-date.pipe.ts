import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

/**
 * Custom Date Pipe để tự động convert UTC sang local timezone
 * 
 * Sử dụng:
 * {{ dateString | localDate: 'dd/MM/yyyy HH:mm' }}
 * 
 * Input: ISO 8601 string từ API (UTC)
 * Output: Date hiển thị theo timezone local của browser
 */
@Pipe({
  name: 'localDate',
  standalone: true
})
export class LocalDatePipe implements PipeTransform {
  private datePipe = new DatePipe('vi');

  transform(value: string | Date | null | undefined, format: string = 'dd/MM/yyyy'): string | null {
    if (!value) {
      return null;
    }

    // Convert string sang Date object nếu cần
    const date = typeof value === 'string' ? new Date(value) : value;

    // Check valid date
    if (isNaN(date.getTime())) {
      return null;
    }

    // DatePipe tự động convert sang timezone local của browser
    return this.datePipe.transform(date, format);
  }
}
