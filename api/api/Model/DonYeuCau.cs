using System.ComponentModel.DataAnnotations.Schema;
using api.Model.Enums;

namespace api.Model
{
    /// <summary>
    /// Model tổng quát cho mọi loại đơn yêu cầu:
    /// - Nghỉ phép
    /// - Làm thêm giờ
    /// - Đi muộn
    /// - Công tác
    /// </summary>
    public class DonYeuCau
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Mã đơn duy nhất để dễ nhận diện (VD: DON-2025-001)
        /// </summary>
        public string? MaDon { get; set; }

        /// <summary>
        /// Loại đơn: NghiPhep, LamThemGio, DiMuon, CongTac
        /// </summary>
        public LoaiDonYeuCau LoaiDon { get; set; }

        /// <summary>
        /// Nhân viên tạo đơn
        /// </summary>
        public Guid NhanVienId { get; set; }

        /// <summary>
        /// Trạng thái: DangChoDuyet, DaChapThuan, BiTuChoi, DaHuy
        /// </summary>
        public TrangThaiDon TrangThai { get; set; } = TrangThaiDon.DangChoDuyet;

        #region Thông tin chung

        /// <summary>
        /// Lý do tạo đơn (bắt buộc cho tất cả loại đơn)
        /// </summary>
        public required string LyDo { get; set; }

        /// <summary>
        /// Ngày bắt đầu (dùng cho NghiPhep, CongTac)
        /// </summary>
        public DateTime? NgayBatDau { get; set; }

        /// <summary>
        /// Ngày kết thúc (dùng cho NghiPhep, CongTac)
        /// </summary>
        public DateTime? NgayKetThuc { get; set; }

        /// <summary>
        /// Loại nghỉ phép chi tiết (chỉ dùng khi LoaiDon = NghiPhep)
        /// </summary>
        public LoaiNghiPhep? LoaiNghiPhep { get; set; }

        #endregion

        #region Dành cho Làm Thêm Giờ

        /// <summary>
        /// Số giờ làm thêm (dùng cho LamThemGio)
        /// </summary>
        public decimal? SoGioLamThem { get; set; }

        /// <summary>
        /// Ngày làm thêm giờ (dùng cho LamThemGio)
        /// </summary>
        public DateTime? NgayLamThem { get; set; }

        #endregion

        #region Dành cho Đi Muộn

        /// <summary>
        /// Giờ dự kiến đến (dùng cho DiMuon)
        /// </summary>
        public DateTime? GioDuKienDen { get; set; }

        /// <summary>
        /// Ngày đi muộn (dùng cho DiMuon)
        /// </summary>
        public DateTime? NgayDiMuon { get; set; }

        #endregion

        #region Dành cho Công Tác

        /// <summary>
        /// Địa điểm công tác (dùng cho CongTac)
        /// </summary>
        public string? DiaDiemCongTac { get; set; }

        /// <summary>
        /// Mục đích công tác (dùng cho CongTac)
        /// </summary>
        public string? MucDichCongTac { get; set; }

        #endregion

        #region Thông tin duyệt đơn

        /// <summary>
        /// Người duyệt đơn (Giám Đốc hoặc Trưởng Phòng)
        /// </summary>
        public Guid? DuocChapThuanBoi { get; set; }

        /// <summary>
        /// Ghi chú của người duyệt (lý do từ chối, góp ý...)
        /// </summary>
        public string? GhiChuNguoiDuyet { get; set; }

        /// <summary>
        /// Ngày duyệt/từ chối đơn
        /// </summary>
        public DateTime? NgayDuyet { get; set; }

        #endregion

        #region Telegram Notification Tracking

        /// <summary>
        /// Đã gửi thông báo Telegram chưa
        /// </summary>
        public bool DaGuiTelegram { get; set; } = false;

        /// <summary>
        /// Thời gian gửi Telegram
        /// </summary>
        public DateTime? ThoiGianGuiTelegram { get; set; }

        /// <summary>
        /// Message ID từ Telegram (để có thể edit/reply sau)
        /// Lưu dưới dạng JSON nếu gửi nhiều người: {"giamdoc": 123, "truongphong": 456}
        /// </summary>
        public string? TelegramMessageIds { get; set; }

        /// <summary>
        /// Lỗi khi gửi Telegram (nếu có)
        /// </summary>
        public string? TelegramError { get; set; }

        #endregion

        #region Audit fields

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime? NgayCapNhat { get; set; }

        #endregion

        #region Navigation Properties

        public virtual NhanVien NhanVien { get; set; } = null!;
        public virtual NhanVien? NguoiDuyet { get; set; }
        public virtual ICollection<ThongBao>? ThongBaos { get; set; }

        #endregion
    }
}
