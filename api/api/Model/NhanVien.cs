using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Model.Enums;

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
        public NhanVienStatus Status { get; set; } = NhanVienStatus.Active;

        public virtual User User { get; set; } = null!;
        public virtual PhongBan? PhongBan { get; set; }
        public virtual ChucVu? ChucVu { get; set; }
        public virtual NhanVien? QuanLy { get; set; }
        public virtual ICollection<DonYeuCau>? DonYeuCaus { get; set; }
    }
}
