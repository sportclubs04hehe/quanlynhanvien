using System.ComponentModel.DataAnnotations.Schema;

namespace api.Model
{
    /// <summary>
    /// Model cho hệ thống thông báo (notifications)
    /// Dùng để thông báo về: đơn yêu cầu, thông báo chung, nhắc nhở...
    /// </summary>
    public class ThongBao
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Loại thông báo: DON_YEU_CAU, THONG_BAO_CHUNG, NHAN_SU, HE_THONG...
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Nhân viên nhận thông báo
        /// </summary>
        public Guid NhanVienId { get; set; }

        /// <summary>
        /// Liên kết đến đơn yêu cầu (nếu thông báo liên quan đến đơn)
        /// Nullable: không phải thông báo nào cũng liên quan đến đơn
        /// </summary>
        public Guid? DonYeuCauId { get; set; }

        /// <summary>
        /// Tiêu đề thông báo
        /// </summary>
        public required string TieuDe { get; set; }

        /// <summary>
        /// Nội dung thông báo
        /// </summary>
        public string? NoiDung { get; set; }

        /// <summary>
        /// Thời gian gửi thông báo
        /// </summary>
        public DateTime ThoiGianGui { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Đã đọc chưa
        /// </summary>
        public bool DaDoc { get; set; } = false;

        /// <summary>
        /// Thời gian đọc (nếu đã đọc)
        /// </summary>
        public DateTime? ThoiGianDoc { get; set; }

        /// <summary>
        /// Link để redirect (optional - cho deep linking)
        /// VD: /don-yeu-cau/123, /thong-bao/456
        /// </summary>
        public string? Link { get; set; }

        #region Telegram Integration

        /// <summary>
        /// Có gửi qua Telegram không (ngoài thông báo trong hệ thống)
        /// </summary>
        public bool GuiQuaTelegram { get; set; } = false;

        /// <summary>
        /// Telegram Message ID (để có thể edit/delete message)
        /// </summary>
        public long? TelegramMessageId { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(NhanVienId))]
        public virtual NhanVien NhanVien { get; set; } = null!;

        [ForeignKey(nameof(DonYeuCauId))]
        public virtual DonYeuCau? DonYeuCau { get; set; }

        #endregion
    }
}