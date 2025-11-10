namespace api.Model
{
    /// <summary>
    /// Model lưu cấu hình Telegram (có thể lưu trong DB hoặc appsettings.json)
    /// Nếu lưu DB thì có thể quản lý nhiều bot hoặc thay đổi config không cần deploy
    /// </summary>
    public class TelegramConfig
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Bot Token từ @BotFather
        /// </summary>
        public required string BotToken { get; set; }

        /// <summary>
        /// Chat ID của nhóm Telegram (hoặc chat riêng) nhận thông báo
        /// Có thể null nếu gửi riêng từng người
        /// </summary>
        public string? GroupChatId { get; set; }

        /// <summary>
        /// Webhook URL (nếu dùng webhook thay vì polling)
        /// </summary>
        public string? WebhookUrl { get; set; }

        /// <summary>
        /// Bật/tắt tính năng Telegram
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Template message cho thông báo đơn xin nghỉ
        /// Có thể customize format tin nhắn
        /// </summary>
        public string? DonNghiPhepTemplate { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime? NgayCapNhat { get; set; }
    }
}
