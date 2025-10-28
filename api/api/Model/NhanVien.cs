using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Model
{
    public class NhanVien
    {
        [Key]
        [ForeignKey(nameof(User))]
        public Guid Id { get; set; }

        public required string TenDayDu { get; set; }

        public Guid? PhongBanId { get; set; }
        public Guid? ChucVuId { get; set; }
        public Guid? QuanLyId { get; set; }

        public DateTime? NgaySinh { get; set; }
        public DateTime? NgayVaoLam { get; set; }

        public string? TelegramChatId { get; set; }
        public string? Status { get; set; }

        public virtual User User { get; set; }
        public virtual PhongBan? PhongBan { get; set; }
        public virtual ChucVu? ChucVu { get; set; }
        public virtual NhanVien? QuanLy { get; set; }
        public virtual ICollection<DonXinNghiPhep>? DonXinNghiPhep { get; set; }
    }
}
