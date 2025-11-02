using api.DTO;

namespace api.Service.Interface
{
    public interface IAuthService
    {
        /// <summary>
        /// Đăng nhập
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(LoginDto dto, string? ipAddress = null);

        /// <summary>
        /// Đăng ký user mới (tạo cả User và NhanVien)
        /// </summary>
        Task<UserDto> RegisterAsync(RegisterUserDto dto);

        /// <summary>
        /// Lấy thông tin user theo ID
        /// </summary>
        Task<UserDto?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Lấy danh sách users với phân trang
        /// </summary>
        Task<PagedResult<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize, string? searchTerm);

        /// <summary>
        /// Cập nhật thông tin user
        /// </summary>
        Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto);

        /// <summary>
        /// Xóa user (xóa cả User và NhanVien)
        /// </summary>
        Task<bool> DeleteUserAsync(Guid id);

        /// <summary>
        /// Refresh access token bằng refresh token
        /// </summary>
        Task<LoginResponseDto?> RefreshTokenAsync(string accessToken, string refreshToken, string? ipAddress = null);

        /// <summary>
        /// Thu hồi (revoke) refresh token
        /// </summary>
        Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null);

        /// <summary>
        /// Thu hồi (revoke) refresh token theo ID (dùng cho revoke single session từ UI)
        /// </summary>
        Task<bool> RevokeTokenByIdAsync(Guid tokenId, Guid userId, string? ipAddress = null);

        /// <summary>
        /// Thu hồi TẤT CẢ refresh tokens của user (dùng khi đổi mật khẩu, phát hiện bất thường)
        /// </summary>
        Task<int> RevokeAllUserTokensAsync(Guid userId, string? ipAddress = null);

        /// <summary>
        /// Lấy danh sách active sessions của user (devices đang login)
        /// </summary>
        Task<List<RefreshTokenInfoDto>> GetActiveSessionsAsync(Guid userId, string? currentRefreshToken = null);

        /// <summary>
        /// Xóa tất cả refresh tokens đã hết hạn của user
        /// </summary>
        Task CleanupExpiredTokensAsync(Guid userId);
    }
}
