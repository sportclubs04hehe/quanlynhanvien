using api.DTO;

namespace api.Service.Interface
{
    /// <summary>
    /// Service xử lý business logic liên kết Telegram
    /// </summary>
    public interface ITelegramLinkService
    {
        /// <summary>
        /// Tạo deep link để liên kết tài khoản với Telegram
        /// </summary>
        /// <param name="nhanVienId">ID nhân viên</param>
        /// <param name="ipAddress">IP address của request</param>
        /// <returns>Thông tin deep link và token</returns>
        Task<TelegramLinkResponseDto> GenerateDeepLinkAsync(Guid nhanVienId, string? ipAddress);

        /// <summary>
        /// Kiểm tra trạng thái liên kết Telegram của nhân viên
        /// </summary>
        /// <param name="nhanVienId">ID nhân viên</param>
        /// <returns>Thông tin trạng thái liên kết</returns>
        Task<TelegramLinkStatusDto> GetLinkStatusAsync(Guid nhanVienId);

        /// <summary>
        /// Hủy liên kết Telegram
        /// </summary>
        /// <param name="nhanVienId">ID nhân viên</param>
        Task UnlinkTelegramAsync(Guid nhanVienId);

        /// <summary>
        /// Xác thực token liên kết
        /// </summary>
        /// <param name="token">Token cần xác thực</param>
        /// <returns>Thông tin xác thực token</returns>
        Task<TokenVerificationDto> VerifyTokenAsync(string token);
    }
}
