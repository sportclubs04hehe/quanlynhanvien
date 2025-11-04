////////////////////////////////////////////////////////////////////////////
//LÊ MINH HUY
////////////////////////////////////////////////////////////////////////////

/**
 * Constants cho roles trong hệ thống
 * Phải khớp chính xác với backend (api/Model/Enums/AppRoles.cs) lệch thì hết cứu
 */
export const APP_ROLES = {
  GIAM_DOC: 'GiamDoc',
  TRUONG_PHONG: 'TruongPhong',
  NHAN_VIEN: 'NhanVien'
} as const;

/**
 * Type-safe role type
 */
export type AppRole = typeof APP_ROLES[keyof typeof APP_ROLES];

/**
 * Helper để display role name tiếng Việt
 */
export const ROLE_DISPLAY_NAMES: Record<AppRole, string> = {
  [APP_ROLES.GIAM_DOC]: 'Giám Đốc',
  [APP_ROLES.TRUONG_PHONG]: 'Trưởng Phòng',
  [APP_ROLES.NHAN_VIEN]: 'Nhân Viên'
};

/**
 * Helper function để lấy display name
 */
export function getRoleDisplayName(role: string): string {
  return ROLE_DISPLAY_NAMES[role as AppRole] || role;
}
