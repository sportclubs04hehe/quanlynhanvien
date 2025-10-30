using api.DTO;

namespace api.Service.Interface
{
    public interface IChucVuService
    {
        Task<PagedResult<ChucVuDto>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<ChucVuDto?> GetByIdAsync(Guid id);
        Task<ChucVuDto> CreateAsync(CreateChucVuDto createDto);
        Task<ChucVuDto?> UpdateAsync(Guid id, UpdateChucVuDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
