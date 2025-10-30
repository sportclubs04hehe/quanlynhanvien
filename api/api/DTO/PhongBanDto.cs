using System.ComponentModel.DataAnnotations;

namespace api.DTO
{
    public class PhongBanDto
    {
        public Guid Id { get; set; }
        public string TenPhongBan { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public int SoLuongNhanVien { get; set; }
    }

    public class UpdatePhongBanDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public required string TenPhongBan { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }
    }

    public class CreatePhongBanDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public required string TenPhongBan { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }
    }
}
