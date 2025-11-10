namespace api.Model
{
    /// <summary>
    /// Lưu trạng thái conversation với user trên Telegram (In-memory)
    /// Dùng để tracking user đang ở bước nào trong quá trình liên kết
    /// </summary>
    public class TelegramUserState
    {
        public long ChatId { get; set; }
        public string CurrentStep { get; set; } = ""; // "awaiting_email", "awaiting_confirmation"
        public string? Email { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Các bước trong conversation
    /// </summary>
    public static class TelegramConversationSteps
    {
        public const string AwaitingEmail = "awaiting_email";
        public const string Completed = "completed";
    }
}
