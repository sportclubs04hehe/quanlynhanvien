import { Injectable } from '@angular/core';
import { NgbDateParserFormatter, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';

/**
 * Custom DateParserFormatter cho ng-bootstrap datepicker
 * Format: dd/MM/yyyy (Tiếng Việt)
 */
@Injectable()
export class CustomDateParserFormatter extends NgbDateParserFormatter {
  /**
   * Parse chuỗi thành NgbDateStruct
   * Input: "31/12/2025" -> {day: 31, month: 12, year: 2025}
   */
  parse(value: string): NgbDateStruct | null {
    if (!value) {
      return null;
    }

    const parts = value.trim().split('/');
    if (parts.length === 3) {
      const day = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10);
      const year = parseInt(parts[2], 10);

      if (!isNaN(day) && !isNaN(month) && !isNaN(year)) {
        return { day, month, year };
      }
    }

    return null;
  }

  /**
   * Format NgbDateStruct thành chuỗi
   * Input: {day: 31, month: 12, year: 2025} -> "31/12/2025"
   */
  format(date: NgbDateStruct | null): string {
    if (!date) {
      return '';
    }

    const day = date.day.toString().padStart(2, '0');
    const month = date.month.toString().padStart(2, '0');
    const year = date.year;

    return `${day}/${month}/${year}`;
  }
}
