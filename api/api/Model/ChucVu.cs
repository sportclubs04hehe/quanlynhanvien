namespace api.Model
{
    public class ChucVu
    {
        public Guid Id { get; set; }
        public required string TenChucVu { get; set; }
        public int Level { get; set; }
        public virtual ICollection<NhanVien>? NhanViens { get; set; }
    }
}