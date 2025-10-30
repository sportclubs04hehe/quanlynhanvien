using api.Model;

namespace api.Repository.Interface
{
    public interface IChucVuRepository
    {
        Task<(List<ChucVu> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<ChucVu?> GetByIdAsync(Guid id);
        Task<ChucVu> CreateAsync(ChucVu ChucVu);
        Task<ChucVu> UpdateAsync(ChucVu ChucVu);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
