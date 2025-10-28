using System.ComponentModel.DataAnnotations.Schema;

namespace api.Model
{
    public class DonXinNghiPhep
    {
        public Guid Id { get; set; }
        public Guid NhanVienId { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public required string LyDo { get; set; }
        public string TrangThai { get; set; } = "DANGCHODUYET";
        public Guid? DuocChapThuanBoi { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime? NgayCapNhat { get; set; }

        public virtual NhanVien NhanVien { get; set; }
        public virtual NhanVien? NguoiDuyet { get; set; }
        public virtual ICollection<ThongBao>? ThongBaos { get; set; }
    }
}