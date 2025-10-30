import { Injectable } from '@angular/core';
import { PagedResult } from '../types/page-result.model';

interface CacheEntry<T> {
  data: PagedResult<T>;
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class CacheService {
  private cache = new Map<string, CacheEntry<any>>();
  private readonly TTL = 5 * 60 * 1000; // 5 phút

  set<T>(key: string, data: PagedResult<T>): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now()
    });
  }

  get<T>(key: string): PagedResult<T> | null {
    const entry = this.cache.get(key);
    
    if (!entry) {
      return null;
    }

    // Kiểm tra xem cache có còn valid không
    if (Date.now() - entry.timestamp > this.TTL) {
      this.cache.delete(key);
      return null;
    }

    return entry.data;
  }

  clear(prefix?: string): void {
    if (prefix) {
      // Xóa tất cả cache có key bắt đầu với prefix
      Array.from(this.cache.keys())
        .filter(key => key.startsWith(prefix))
        .forEach(key => this.cache.delete(key));
    } else {
      // Xóa toàn bộ cache
      this.cache.clear();
    }
  }

  has(key: string): boolean {
    const entry = this.cache.get(key);
    if (!entry) return false;
    
    // Kiểm tra TTL
    if (Date.now() - entry.timestamp > this.TTL) {
      this.cache.delete(key);
      return false;
    }
    
    return true;
  }
}
