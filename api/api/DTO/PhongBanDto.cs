namespace api.DTO
{
    public class PhongBanDto
    {
        public Guid Id { get; set; }
        public string TenPhongBan { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public int SoLuongNhanVien { get; set; }
    }
}
