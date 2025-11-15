import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Pipe để highlight text tìm kiếm trong chuỗi
 * Usage: {{ text | highlight: searchTerm }}
 * 
 * @example
 * {{ 'Đơn nghỉ phép' | highlight: 'nghỉ' }}
 * Output: Đơn <mark class="highlight">nghỉ</mark> phép
 */
@Pipe({
  name: 'highlight',
  standalone: true,
  pure: true // Pure pipe for better performance
})
export class HighlightPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) {}

  transform(value: string, searchTerm: string): SafeHtml {
    // Nếu không có value hoặc searchTerm, trả về value gốc
    if (!value || !searchTerm) {
      return value || '';
    }

    // Trim và check empty
    const search = searchTerm.trim();
    if (!search) {
      return value;
    }

    // Escape special regex characters
    const escapedSearch = search.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    
    // Create regex with case-insensitive flag and global flag
    const regex = new RegExp(escapedSearch, 'gi');
    
    // Replace matched text with highlighted version
    const highlighted = value.replace(regex, (match) => {
      return `<mark class="bg-warning text-dark px-1 rounded">${match}</mark>`;
    });

    // Sanitize and return
    return this.sanitizer.sanitize(1, highlighted) || highlighted;
  }
}
