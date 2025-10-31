using api.Model;

namespace api.Repository.Interface
{
    public interface INhanVienRepository
    {
        Task<(List<NhanVien> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<NhanVien?> GetByIdAsync(Guid id);
        Task<NhanVien> CreateAsync(NhanVien nhanVien);
        Task<NhanVien> UpdateAsync(NhanVien nhanVien);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
