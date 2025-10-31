using System.ComponentModel.DataAnnotations;

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
        public string? Status { get; set; }
        public DateTime? NgaySinh { get; set; }
        public DateTime? NgayVaoLam { get; set; }
        public string? TelegramChatId { get; set; }

        // Thông tin liên quan
        public PhongBanDto? PhongBan { get; set; }
        public ChucVuDto? ChucVu { get; set; }
        public string? TenQuanLy { get; set; }
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
        public string? Status { get; set; }
    }
}
