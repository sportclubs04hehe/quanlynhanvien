using api.DTO;

namespace api.Service.Interface
{
    public interface IPhongBanService
    {
        Task<PagedResult<PhongBanDto>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<PhongBanDto?> GetByIdAsync(Guid id);
        Task<PhongBanDto> CreateAsync(CreatePhongBanDto createDto);
        Task<PhongBanDto?> UpdateAsync(Guid id, UpdatePhongBanDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
