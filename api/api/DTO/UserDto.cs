using System.ComponentModel.DataAnnotations;
using api.Model.Enums;

namespace api.DTO
{
    /// <summary>
    /// DTO để đăng ký user mới (User + NhanVien)
    /// </summary>
    public class RegisterUserDto
    {
        // Thông tin User (Identity)
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public required string Password { get; set; }

        public string? Role { get; set; }

        // Thông tin NhanVien
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự")]
        public required string TenDayDu { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public Guid? PhongBanId { get; set; }
        public Guid? ChucVuId { get; set; }
        public Guid? QuanLyId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayVaoLam { get; set; }

        public string? TelegramChatId { get; set; }
    }

    /// <summary>
    /// DTO trả về thông tin user (User + NhanVien)
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string TenDayDu { get; set; } = string.Empty;
        public NhanVienStatus Status { get; set; }
        public DateTime? NgaySinh { get; set; }
        public DateTime? NgayVaoLam { get; set; }
        public string? TelegramChatId { get; set; }

        // Thông tin liên quan
        public Guid? PhongBanId { get; set; }
        public PhongBanDto? PhongBan { get; set; }
        
        public Guid? ChucVuId { get; set; }
        public ChucVuDto? ChucVu { get; set; }
        
        public Guid? QuanLyId { get; set; }
        public string? TenQuanLy { get; set; }
        
        // Role
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// DTO để cập nhật thông tin user
    /// </summary>
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự")]
        public required string TenDayDu { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public Guid? PhongBanId { get; set; }
        public Guid? ChucVuId { get; set; }
        public Guid? QuanLyId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NgayVaoLam { get; set; }

        public string? TelegramChatId { get; set; }
        public NhanVienStatus? Status { get; set; }
        
        /// <summary>
        /// Role để cập nhật (chỉ Giám Đốc mới được đổi role)
        /// </summary>
        public string? Role { get; set; }
    }

    /// <summary>
    /// DTO để đăng nhập
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// DTO trả về sau khi đăng nhập thành công
    /// </summary>
    public class LoginResponseDto
    {
        public string TokenType { get; set; } = "Bearer";        // Loại token (thường là Bearer)
        public string AccessToken { get; set; } = string.Empty;  // Mã truy cập JWT
        public int ExpiresIn { get; set; }                       // Thời gian hết hạn (tính bằng giây)
        public string RefreshToken { get; set; } = string.Empty; // Token để làm mới AccessToken
        public UserDto User { get; set; } = null!;               // Thông tin người dùng đăng nhập
    }

    /// <summary>
    /// DTO để refresh access token
    /// </summary>
    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "Access token là bắt buộc")]
        public required string AccessToken { get; set; }

        [Required(ErrorMessage = "Refresh token là bắt buộc")]
        public required string RefreshToken { get; set; }
    }

    /// <summary>
    /// DTO để revoke refresh token
    /// </summary>
    public class RevokeTokenRequestDto
    {
        [Required(ErrorMessage = "Refresh token là bắt buộc")]
        public required string RefreshToken { get; set; }
    }

    /// <summary>
    /// DTO thông tin session (device đang login)
    /// </summary>
    public class RefreshTokenInfoDto
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty; // Chỉ hiển thị 10 ký tự cuối
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? CreatedByIp { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentSession { get; set; } // Token hiện tại user đang dùng
    }

}
