namespace api.Model.Enums
{
    /// <summary>
    /// Enum định nghĩa các loại nghỉ phép chi tiết
    /// </summary>
    public enum LoaiNghiPhep
    {
        BuoiSang = 1,       // Nghỉ buổi sáng (nửa ngày)
        BuoiChieu = 2,      // Nghỉ buổi chiều (nửa ngày)
        MotNgay = 3,        // Nghỉ 1 ngày (cả ngày)
        NhieuNgay = 4       // Nghỉ nhiều ngày (từ 2 ngày trở lên)
    }

    /// <summary>
    /// Extension methods cho LoaiNghiPhep
    /// </summary>
    public static class LoaiNghiPhepExtensions
    {
        /// <summary>
        /// Lấy tên hiển thị tiếng Việt
        /// </summary>
        public static string ToDisplayName(this LoaiNghiPhep loai)
        {
            return loai switch
            {
                LoaiNghiPhep.BuoiSang => "Buổi Sáng",
                LoaiNghiPhep.BuoiChieu => "Buổi Chiều",
                LoaiNghiPhep.MotNgay => "Một Ngày",
                LoaiNghiPhep.NhieuNgay => "Nhiều Ngày",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Kiểm tra có phải nghỉ nửa ngày hay không
        /// </summary>
        public static bool IsHalfDay(this LoaiNghiPhep loai)
        {
            return loai == LoaiNghiPhep.BuoiSang || loai == LoaiNghiPhep.BuoiChieu;
        }

        /// <summary>
        /// Kiểm tra có phải nghỉ cả ngày hay không
        /// </summary>
        public static bool IsFullDay(this LoaiNghiPhep loai)
        {
            return loai == LoaiNghiPhep.MotNgay || loai == LoaiNghiPhep.NhieuNgay;
        }

        /// <summary>
        /// Lấy số ngày nghỉ tối thiểu
        /// </summary>
        public static decimal GetMinimumDays(this LoaiNghiPhep loai)
        {
            return loai switch
            {
                LoaiNghiPhep.BuoiSang => 0.5m,
                LoaiNghiPhep.BuoiChieu => 0.5m,
                LoaiNghiPhep.MotNgay => 1m,
                LoaiNghiPhep.NhieuNgay => 2m,
                _ => 0m
            };
        }
    }
}
