using api.DTO;

namespace api.Service.Interface
{
    public interface IAuthService
    {
        /// <summary>
        /// Đăng nhập
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);

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
    }
}
