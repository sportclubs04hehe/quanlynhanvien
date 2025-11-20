using System.ComponentModel.DataAnnotations;

namespace api.Model
{
    /// <summary>
    /// Quản lý định mức ngày phép của nhân viên theo tháng
    /// Policy: Mỗi tháng được nghỉ 1 ngày, không nghỉ thì mất (không chuyển sang tháng sau)
    /// </summary>
    public class NghiPhepQuota
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Nhân viên
        /// </summary>
        public Guid NhanVienId { get; set; }

        /// <summary>
        /// Năm áp dụng (VD: 2025)
        /// </summary>
        public int Nam { get; set; }

        /// <summary>
        /// Tháng áp dụng (1-12)
        /// </summary>
        public int Thang { get; set; }

        /// <summary>
        /// Số ngày phép được phép trong tháng (default: 1 ngày)
        /// Có thể thay đổi theo cấu hình của Giám Đốc
        /// </summary>
        public decimal SoNgayPhepThang { get; set; } = 1m;

        /// <summary>
        /// Số ngày phép đã sử dụng trong tháng (tính từ đơn đã approved)
        /// </summary>
        public decimal SoNgayDaSuDung { get; set; } = 0m;

        /// <summary>
        /// Tổng số giờ làm thêm trong tháng (đã được approved)
        /// Chỉ để tracking, không quy đổi thành phép
        /// </summary>
        public decimal TongSoGioLamThem { get; set; } = 0m;

        /// <summary>
        /// Ghi chú (VD: "Tháng này tăng lên 2 ngày do dự án đặc biệt")
        /// </summary>
        public string? GhiChu { get; set; }

        #region Audit Fields

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime? NgayCapNhat { get; set; }

        #endregion

        #region Navigation Properties

        public virtual NhanVien NhanVien { get; set; } = null!;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Số ngày phép còn lại trong tháng
        /// </summary>
        public decimal SoNgayPhepConLai => SoNgayPhepThang - SoNgayDaSuDung;

        /// <summary>
        /// Đã vượt quota chưa
        /// </summary>
        public bool DaVuotQuota => SoNgayDaSuDung > SoNgayPhepThang;

        #endregion
    }
}
