using System.ComponentModel.DataAnnotations;

namespace api.Model
{
    public class PhongBan
    {
        [Key]
        public Guid Id { get; set; }
        public required string TenPhongBan { get; set; }
        public string? MoTa { get; set; }
        public virtual ICollection<NhanVien>? NhanViens { get; set; }
    }
}