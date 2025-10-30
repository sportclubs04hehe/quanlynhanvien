using System.ComponentModel.DataAnnotations;

namespace api.DTO
{
    public class ChucVuDto
    {
        public Guid Id { get; set; }
        public string TenChucVu { get; set; } = string.Empty;
        public int Level { get; set; }
        public int SoLuongNhanVien { get; set; }
    }

    public class CreateChucVuDto
    {
        [Required(ErrorMessage = "Tên chức vụ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chức vụ không được vượt quá 100 ký tự")]
        public required string TenChucVu { get; set; }

        [Required(ErrorMessage = "Level là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Level phải từ 1 đến 10")]
        public int Level { get; set; }
    }

    public class UpdateChucVuDto
    {
        [Required(ErrorMessage = "Tên chức vụ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chức vụ không được vượt quá 100 ký tự")]
        public required string TenChucVu { get; set; }

        [Required(ErrorMessage = "Level là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Level phải từ 1 đến 10")]
        public int Level { get; set; }
    }
}

