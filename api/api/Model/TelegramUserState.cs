namespace api.Model
{
    public class TelegramUserState
    {
        // Properties cho duyệt đơn qua Telegram
        public string? State { get; set; } // "WAITING_REJECT_REASON"
        public Guid? DonIdToReject { get; set; } // ID đơn đang chờ nhập lý do từ chối
    }
}
