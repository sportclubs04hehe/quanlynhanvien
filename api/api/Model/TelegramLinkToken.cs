namespace api.Model
{
    /// <summary>
    /// Token để xác thực Deep Link liên kết Telegram
    /// </summary>
    public class TelegramLinkToken
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// Token duy nhất (UUID)
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// ID nhân viên yêu cầu liên kết
        /// </summary>
        public Guid NhanVienId { get; set; }
        
        /// <summary>
        /// Navigation property
        /// </summary>
        public NhanVien? NhanVien { get; set; }
        
        /// <summary>
        /// Thời điểm tạo token
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Thời điểm hết hạn (thường 5-10 phút)
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Đã được sử dụng chưa (one-time use)
        /// </summary>
        public bool IsUsed { get; set; } = false;
        
        /// <summary>
        /// Thời điểm sử dụng token
        /// </summary>
        public DateTime? UsedAt { get; set; }
        
        /// <summary>
        /// ChatId của Telegram sau khi liên kết
        /// </summary>
        public long? TelegramChatId { get; set; }
        
        /// <summary>
        /// IP address của user khi tạo token (for audit)
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
