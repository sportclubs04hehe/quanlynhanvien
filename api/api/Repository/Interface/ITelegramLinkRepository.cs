using api.Model;

namespace api.Repository.Interface
{
    /// <summary>
    /// Repository xử lý dữ liệu TelegramLinkToken
    /// </summary>
    public interface ITelegramLinkRepository
    {
        /// <summary>
        /// Tìm token theo chuỗi token
        /// </summary>
        Task<TelegramLinkToken?> GetByTokenAsync(string token, bool includeNhanVien = false);

        /// <summary>
        /// Tìm các token chưa sử dụng của nhân viên
        /// </summary>
        Task<List<TelegramLinkToken>> GetUnusedTokensByNhanVienAsync(Guid nhanVienId);

        /// <summary>
        /// Tìm token pending (chưa dùng, chưa hết hạn) mới nhất của nhân viên
        /// </summary>
        Task<TelegramLinkToken?> GetPendingTokenAsync(Guid nhanVienId);

        /// <summary>
        /// Tạo token mới
        /// </summary>
        Task<TelegramLinkToken> CreateAsync(TelegramLinkToken token);

        /// <summary>
        /// Xóa nhiều tokens
        /// </summary>
        Task DeleteRangeAsync(List<TelegramLinkToken> tokens);

        /// <summary>
        /// Cập nhật token (đánh dấu đã sử dụng, etc.)
        /// </summary>
        Task UpdateAsync(TelegramLinkToken token);

        /// <summary>
        /// Lưu thay đổi
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
