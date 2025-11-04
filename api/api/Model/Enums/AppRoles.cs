namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa các vai trò trong hệ thống
    /// </summary>
    public enum AppRoles
    {
        GiamDoc = 1,        // Giám Đốc - Full quyền
        TruongPhong = 2,    // Trưởng Phòng - Quản lý phòng ban
        NhanVien = 3        // Nhân Viên - Quyền cơ bản
    }

    /// <summary>
    /// Extension methods và constants cho Roles
    /// </summary>
    public static class AppRolesExtensions
    {
        public const string GiamDoc = "GiamDoc";
        public const string TruongPhong = "TruongPhong";
        public const string NhanVien = "NhanVien";

        // Combine roles
        public const string GiamDocOrTruongPhong = "GiamDoc,TruongPhong";
        public const string AllRoles = "GiamDoc,TruongPhong,NhanVien";

        /// <summary>
        /// Lấy tên role từ enum
        /// </summary>
        public static string ToRoleName(this AppRoles role)
        {
            return role switch
            {
                AppRoles.GiamDoc => GiamDoc,
                AppRoles.TruongPhong => TruongPhong,
                AppRoles.NhanVien => NhanVien,
                _ => throw new ArgumentException("Invalid role")
            };
        }

        /// <summary>
        /// Lấy mô tả tiếng Việt
        /// </summary>
        public static string ToDisplayName(this AppRoles role)
        {
            return role switch
            {
                AppRoles.GiamDoc => "Giám Đốc",
                AppRoles.TruongPhong => "Trưởng Phòng",
                AppRoles.NhanVien => "Nhân Viên",
                _ => "Unknown"
            };
        }
    }
}
