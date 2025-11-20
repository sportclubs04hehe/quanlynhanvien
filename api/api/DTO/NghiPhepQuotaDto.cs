namespace api.DTO
{
    /// <summary>
    /// DTO trả về thông tin quota nghỉ phép của nhân viên theo tháng
    /// </summary>
    public class NghiPhepQuotaDto
    {
        public Guid Id { get; set; }
        public Guid NhanVienId { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public int Nam { get; set; }
        public int Thang { get; set; }

        // Quota tháng
        public decimal SoNgayPhepThang { get; set; }
        public decimal SoNgayDaSuDung { get; set; }
        public decimal SoNgayPhepConLai { get; set; }
        public bool DaVuotQuota { get; set; }

        // Làm thêm giờ
        public decimal TongSoGioLamThem { get; set; }

        public string? GhiChu { get; set; }
    }

    /// <summary>
    /// DTO cho Calendar view - thông tin ngày nghỉ đã approved
    /// </summary>
    public class LichNghiCalendarDto
    {
        /// <summary>
        /// Năm - Tháng hiển thị (VD: 2025-11)
        /// </summary>
        public int Nam { get; set; }
        public int Thang { get; set; }

        /// <summary>
        /// Danh sách ngày đã nghỉ trong tháng (sử dụng NgayNghiInfoDto từ DonYeuCauDto.cs)
        /// </summary>
        public List<NgayNghiDetailDto> NgayDaNghi { get; set; } = new();

        /// <summary>
        /// Quota info cho tháng này
        /// </summary>
        public decimal SoNgayNghiTrongThang { get; set; }
        public decimal SoGioLamThemTrongThang { get; set; }
    }

    /// <summary>
    /// DTO thông tin chi tiết một ngày nghỉ với đầy đủ context
    /// </summary>
    public class NgayNghiDetailDto
    {
        public DateTime Ngay { get; set; }
        public Guid DonYeuCauId { get; set; }
        public string MaDon { get; set; } = string.Empty;
        public string LoaiDon { get; set; } = string.Empty; // "NghiPhep", "CongTac"...
        public string? LoaiNghiPhep { get; set; } // "BuoiSang", "BuoiChieu", "MotNgay"...
        public decimal SoNgay { get; set; } // 0.5 hoặc 1
        public string LyDo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO tạo/update quota
    /// </summary>
    public class UpsertNghiPhepQuotaDto
    {
        public Guid NhanVienId { get; set; }
        public int Nam { get; set; }
        public int Thang { get; set; }
        public decimal SoNgayPhepThang { get; set; } = 1m;
        public string? GhiChu { get; set; }
    }

    /// <summary>
    /// DTO dashboard tổng hợp cho tab "Lịch Nghỉ & Công Việc"
    /// </summary>
    public class LichNghiDashboardDto
    {
        /// <summary>
        /// Thông tin quota tháng hiện tại
        /// </summary>
        public NghiPhepQuotaDto? QuotaThangHienTai { get; set; }

        /// <summary>
        /// Tổng số ngày đã nghỉ trong năm (để so sánh với 12 ngày/năm)
        /// </summary>
        public decimal TongNgayNghiTrongNam { get; set; }

        /// <summary>
        /// Tổng số giờ làm thêm trong năm
        /// </summary>
        public decimal TongGioLamThemTrongNam { get; set; }

        /// <summary>
        /// Calendar view tháng hiện tại
        /// </summary>
        public LichNghiCalendarDto? CalendarThangHienTai { get; set; }

        /// <summary>
        /// Danh sách đơn nghỉ sắp tới (approved, chưa đến ngày)
        /// </summary>
        public List<DonYeuCauDto> DonNghiSapToi { get; set; } = new();

        /// <summary>
        /// Cảnh báo nếu có
        /// </summary>
        public List<string> CanhBao { get; set; } = new();
    }
}
