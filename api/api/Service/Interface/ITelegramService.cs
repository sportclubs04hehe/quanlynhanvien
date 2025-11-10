using api.Model;

namespace api.Service.Interface
{
    /// <summary>
    /// Service gửi thông báo qua Telegram Bot
    /// </summary>
    public interface ITelegramService
    {
        /// <summary>
        /// Gửi thông báo đơn xin nghỉ đến giám đốc/người quản lý
        /// </summary>
        /// <param name="donYeuCau">Đơn yêu cầu</param>
        /// <param name="nguoiGui">Nhân viên gửi đơn</param>
        /// <returns>Dictionary chứa ChatId và MessageId đã gửi</returns>
        Task<Dictionary<string, long>> GuiThongBaoDonXinNghiAsync(DonYeuCau donYeuCau, NhanVien nguoiGui);

        /// <summary>
        /// Cập nhật message Telegram khi đơn được duyệt/từ chối
        /// </summary>
        Task CapNhatTrangThaiDonAsync(DonYeuCau donYeuCau, NhanVien nguoiDuyet);

        /// <summary>
        /// Gửi tin nhắn tùy chỉnh đến một ChatId
        /// </summary>
        Task<long?> GuiTinNhanAsync(string chatId, string message);

        /// <summary>
        /// Kiểm tra bot có hoạt động không
        /// </summary>
        Task<bool> KiemTraKetNoiAsync();

        /// <summary>
        /// Bắt đầu lắng nghe messages từ Telegram (Polling)
        /// </summary>
        Task StartReceivingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Dừng lắng nghe messages
        /// </summary>
        Task StopReceivingAsync();
    }
}
