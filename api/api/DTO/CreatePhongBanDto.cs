using System.ComponentModel.DataAnnotations;

namespace api.DTO
{
    public class CreatePhongBanDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public required string TenPhongBan { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }
    }
}
