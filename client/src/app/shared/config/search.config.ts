/**
 * Cấu hình chung cho tìm kiếm và debounce trong toàn ứng dụng
 */

/**
 * Thời gian debounce cho tìm kiếm (milliseconds)
 * - 500ms: Thời gian tối ưu cho search input
 * - Đủ nhanh để UX tốt, đủ chậm để tránh spam API
 */
export const SEARCH_DEBOUNCE_TIME = 500;

/**
 * Thời gian debounce cho auto-save (milliseconds)
 */
export const AUTO_SAVE_DEBOUNCE_TIME = 1000;

/**
 * Thời gian debounce cho resize/scroll events (milliseconds)
 */
export const RESIZE_DEBOUNCE_TIME = 200;

/**
 * Page size mặc định cho pagination
 */
export const DEFAULT_PAGE_SIZE = 10;

/**
 * Số kết quả tối đa cho autocomplete
 */
export const MAX_AUTOCOMPLETE_RESULTS = 10;
