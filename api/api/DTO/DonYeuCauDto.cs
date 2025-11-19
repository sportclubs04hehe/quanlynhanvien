using System.ComponentModel.DataAnnotations;
using api.Model.Enums;

namespace api.DTO
{
    /// <summary>
    /// DTO để tạo đơn yêu cầu mới
    /// </summary>
    public class CreateDonYeuCauDto
    {
        [Required(ErrorMessage = "Loại đơn là bắt buộc")]
        public LoaiDonYeuCau LoaiDon { get; set; }

        [Required(ErrorMessage = "Lý do là bắt buộc")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public required string LyDo { get; set; }

        // Dành cho Nghỉ Phép - Loại nghỉ chi tiết (bắt buộc nếu LoaiDon = NghiPhep)
        public LoaiNghiPhep? LoaiNghiPhep { get; set; }

        // Dành cho Nghỉ Phép và Công Tác
        [DataType(DataType.Date)]
        public DateTime? NgayBatDau { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayKetThuc { get; set; }

        // Dành cho Làm Thêm Giờ
        [Range(0.5, 24, ErrorMessage = "Số giờ làm thêm phải từ 0.5 đến 24 giờ")]
        public decimal? SoGioLamThem { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayLamThem { get; set; }

        // Dành cho Đi Muộn
        [DataType(DataType.DateTime)]
        public DateTime? GioDuKienDen { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayDiMuon { get; set; }

        // Dành cho Công Tác
        [StringLength(200, ErrorMessage = "Địa điểm công tác không được vượt quá 200 ký tự")]
        public string? DiaDiemCongTac { get; set; }

        [StringLength(500, ErrorMessage = "Mục đích công tác không được vượt quá 500 ký tự")]
        public string? MucDichCongTac { get; set; }
    }

    /// <summary>
    /// DTO để cập nhật đơn yêu cầu (chỉ cho đơn đang chờ duyệt)
    /// </summary>
    public class UpdateDonYeuCauDto
    {
        [Required(ErrorMessage = "Lý do là bắt buộc")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public required string LyDo { get; set; }

        // Dành cho Nghỉ Phép - Loại nghỉ chi tiết
        public LoaiNghiPhep? LoaiNghiPhep { get; set; }

        // Dành cho Nghỉ Phép và Công Tác
        [DataType(DataType.Date)]
        public DateTime? NgayBatDau { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayKetThuc { get; set; }

        // Dành cho Làm Thêm Giờ
        [Range(0.5, 24, ErrorMessage = "Số giờ làm thêm phải từ 0.5 đến 24 giờ")]
        public decimal? SoGioLamThem { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayLamThem { get; set; }

        // Dành cho Đi Muộn
        [DataType(DataType.DateTime)]
        public DateTime? GioDuKienDen { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayDiMuon { get; set; }

        // Dành cho Công Tác
        [StringLength(200, ErrorMessage = "Địa điểm công tác không được vượt quá 200 ký tự")]
        public string? DiaDiemCongTac { get; set; }

        [StringLength(500, ErrorMessage = "Mục đích công tác không được vượt quá 500 ký tự")]
        public string? MucDichCongTac { get; set; }
    }

    /// <summary>
    /// DTO trả về thông tin đơn yêu cầu
    /// </summary>
    public class DonYeuCauDto
    {
        public Guid Id { get; set; }
        public string? MaDon { get; set; }
        public LoaiDonYeuCau LoaiDon { get; set; }
        public string LoaiDonText { get; set; } = string.Empty; // Tên hiển thị
        public TrangThaiDon TrangThai { get; set; }
        public string TrangThaiText { get; set; } = string.Empty; // Tên hiển thị

        // Loại nghỉ phép chi tiết (chỉ có khi LoaiDon = NghiPhep)
        public LoaiNghiPhep? LoaiNghiPhep { get; set; }
        public string? LoaiNghiPhepText { get; set; } // Tên hiển thị

        // Thông tin nhân viên
        public Guid NhanVienId { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string? EmailNhanVien { get; set; }
        public PhongBanDto? PhongBan { get; set; }
        public ChucVuDto? ChucVu { get; set; }

        // Thông tin đơn
        public string LyDo { get; set; } = string.Empty;
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public int? SoNgay { get; set; } // Tính toán số ngày (0 = nửa ngày, 1+ = ngày đầy đủ)
        public decimal? SoNgayThucTe { get; set; } // Số ngày thực tế (0.5 cho buổi sáng/chiều)

        // Làm thêm giờ
        public decimal? SoGioLamThem { get; set; }
        public DateTime? NgayLamThem { get; set; }

        // Đi muộn
        public DateTime? GioDuKienDen { get; set; }
        public DateTime? NgayDiMuon { get; set; }

        // Công tác
        public string? DiaDiemCongTac { get; set; }
        public string? MucDichCongTac { get; set; }

        // Thông tin duyệt
        public Guid? DuocChapThuanBoi { get; set; }
        public string? TenNguoiDuyet { get; set; }
        public string? GhiChuNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }

        // Audit
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }

    /// <summary>
    /// DTO để duyệt/từ chối đơn
    /// </summary>
    public class DuyetDonYeuCauDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public TrangThaiDon TrangThai { get; set; } // DaChapThuan hoặc BiTuChoi

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? GhiChuNguoiDuyet { get; set; }
    }

    /// <summary>
    /// DTO để filter/search đơn yêu cầu
    /// </summary>
    public class FilterDonYeuCauDto
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Search
        public string? SearchTerm { get; set; } // Tìm theo tên nhân viên, lý do
        public string? MaDon { get; set; } // Lọc theo mã đơn

        // Filter
        public LoaiDonYeuCau? LoaiDon { get; set; }
        public LoaiNghiPhep? LoaiNghiPhep { get; set; } // Lọc theo loại nghỉ phép (chỉ dùng khi LoaiDon = NghiPhep)
        public TrangThaiDon? TrangThai { get; set; }
        public Guid? NhanVienId { get; set; } // Lọc theo nhân viên cụ thể
        public Guid? NguoiDuyetId { get; set; } // Lọc theo người duyệt
        public Guid? PhongBanId { get; set; } // Lọc theo phòng ban

        // Date range
        [DataType(DataType.Date)]
        public DateTime? TuNgay { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DenNgay { get; set; }
    }

    /// <summary>
    /// DTO thống kê đơn yêu cầu
    /// </summary>
    public class ThongKeDonYeuCauDto
    {
        public int TongSoDon { get; set; }
        public int DangChoDuyet { get; set; }
        public int DaChapThuan { get; set; }
        public int BiTuChoi { get; set; }
        public int DaHuy { get; set; }

        // Thống kê theo loại
        public int SoDonNghiPhep { get; set; }
        public int SoDonLamThemGio { get; set; }
        public int SoDonDiMuon { get; set; }
        public int SoDonCongTac { get; set; }
    }
}
