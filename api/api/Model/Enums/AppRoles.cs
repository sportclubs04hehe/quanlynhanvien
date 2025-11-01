namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa các vai trò trong hệ thống
    /// </summary>
    public enum AppRoles
    {
        GiamDoc = 1,        // Giám Đốc - Full quyền
        PhoGiamDoc = 2,     // Phó Giám Đốc - Quản lý cấp cao
        NhanVien = 3        // Nhân Viên - Quyền cơ bản
    }

    /// <summary>
    /// Extension methods và constants cho Roles
    /// </summary>
    public static class AppRolesExtensions
    {
        public const string GiamDoc = "GiamDoc";
        public const string PhoGiamDoc = "PhoGiamDoc";
        public const string NhanVien = "NhanVien";

        // Combine roles
        public const string GiamDocOrPhoGiamDoc = "GiamDoc,PhoGiamDoc";
        public const string AllRoles = "GiamDoc,PhoGiamDoc,NhanVien";

        /// <summary>
        /// Lấy tên role từ enum
        /// </summary>
        public static string ToRoleName(this AppRoles role)
        {
            return role switch
            {
                AppRoles.GiamDoc => GiamDoc,
                AppRoles.PhoGiamDoc => PhoGiamDoc,
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
                AppRoles.PhoGiamDoc => "Phó Giám Đốc",
                AppRoles.NhanVien => "Nhân Viên",
                _ => "Unknown"
            };
        }
    }
}
