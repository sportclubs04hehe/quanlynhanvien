using api.Model;

namespace api.Repository.Interface
{
    public interface IPhongBanRepository
    {
        Task<(List<PhongBan> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<PhongBan?> GetByIdAsync(Guid id);
        Task<PhongBan> CreateAsync(PhongBan phongBan);
        Task<PhongBan> UpdateAsync(PhongBan phongBan);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
