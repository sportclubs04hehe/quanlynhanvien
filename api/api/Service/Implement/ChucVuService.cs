using api.DTO;
using api.Model;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;

namespace api.Service.Implement
{
    public class ChucVuService : IChucVuService
    {
        private readonly IChucVuRepository _repository;
        private readonly IMapper _mapper;

        public ChucVuService(IChucVuRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ChucVuDto>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var (items, totalCount) = await _repository.GetAllAsync(pageNumber, pageSize, searchTerm);
            
            var dtos = _mapper.Map<List<ChucVuDto>>(items);

            return new PagedResult<ChucVuDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<ChucVuDto?> GetByIdAsync(Guid id)
        {
            var chucVu = await _repository.GetByIdAsync(id);
            return chucVu == null ? null : _mapper.Map<ChucVuDto>(chucVu);
        }

        public async Task<ChucVuDto> CreateAsync(CreateChucVuDto createDto)
        {
            var chucVu = _mapper.Map<ChucVu>(createDto);
            chucVu.Id = Guid.NewGuid();
            
            var created = await _repository.CreateAsync(chucVu);
            return _mapper.Map<ChucVuDto>(created);
        }

        public async Task<ChucVuDto?> UpdateAsync(Guid id, UpdateChucVuDto updateDto)
        {
            var existingChucVu = await _repository.GetByIdAsync(id);
            if (existingChucVu == null)
                return null;

            _mapper.Map(updateDto, existingChucVu);
            
            var updated = await _repository.UpdateAsync(existingChucVu);
            return _mapper.Map<ChucVuDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
