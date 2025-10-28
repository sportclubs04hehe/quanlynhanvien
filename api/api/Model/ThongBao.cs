using System.ComponentModel.DataAnnotations.Schema;

namespace api.Model
{
    public class ThongBao
    {
        public Guid Id { get; set; }
        public required string Type { get; set; }
        public Guid NhanVienId { get; set; }
        public Guid DonXinNghiPhepId { get; set; }

        public DateTime DaGuiLuc { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "PENDING";

        public string? Message { get; set; }

        [ForeignKey(nameof(NhanVienId))]
        public virtual NhanVien NhanVien { get; set; }

        [ForeignKey(nameof(DonXinNghiPhepId))]
        public virtual DonXinNghiPhep DonXinNghiPhep { get; set; }
    }
}