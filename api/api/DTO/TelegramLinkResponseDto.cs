namespace api.DTO
{
    /// <summary>
    /// Response khi tạo link liên kết Telegram
    /// </summary>
    public class TelegramLinkResponseDto
    {
        /// <summary>
        /// Deep link để mở Telegram Bot
        /// Ví dụ: https://t.me/company_manager_sp_bot?start=abc123def456
        /// </summary>
        public string DeepLink { get; set; } = string.Empty;
        
        /// <summary>
        /// Token (để hiển thị cho user nếu cần)
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// Thời gian hết hạn
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Số giây còn lại
        /// </summary>
        public int ExpiresInSeconds { get; set; }
    }

     /// <summary>
    /// DTO cho trạng thái liên kết Telegram
    /// </summary>
    public class TelegramLinkStatusDto
    {
        public bool IsLinked { get; set; }
        public string? ChatId { get; set; }
        public bool HasPendingToken { get; set; }
        public DateTime? PendingTokenExpiresAt { get; set; }
        public int PendingTokenExpiresInSeconds { get; set; }
    }

    /// <summary>
    /// DTO cho kết quả xác thực token
    /// </summary>
    public class TokenVerificationDto
    {
        public bool Valid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NhanVienName { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? ExpiresInSeconds { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
