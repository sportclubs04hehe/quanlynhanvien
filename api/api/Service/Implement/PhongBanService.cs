using api.DTO;
using api.Model;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;

namespace api.Service.Implement
{
    public class PhongBanService : IPhongBanService
    {
        private readonly IPhongBanRepository _repository;
        private readonly IMapper _mapper;

        public PhongBanService(IPhongBanRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<PhongBanDto>> GetAllAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var (items, totalCount) = await _repository.GetAllAsync(pageNumber, pageSize, searchTerm);
            
            var dtos = _mapper.Map<List<PhongBanDto>>(items);

            return new PagedResult<PhongBanDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PhongBanDto?> GetByIdAsync(Guid id)
        {
            var phongBan = await _repository.GetByIdAsync(id);
            return phongBan == null ? null : _mapper.Map<PhongBanDto>(phongBan);
        }

        public async Task<PhongBanDto> CreateAsync(CreatePhongBanDto createDto)
        {
            var phongBan = _mapper.Map<PhongBan>(createDto);
            phongBan.Id = Guid.NewGuid();
            
            var created = await _repository.CreateAsync(phongBan);
            return _mapper.Map<PhongBanDto>(created);
        }

        public async Task<PhongBanDto?> UpdateAsync(Guid id, UpdatePhongBanDto updateDto)
        {
            var existingPhongBan = await _repository.GetByIdAsync(id);
            if (existingPhongBan == null)
                return null;

            _mapper.Map(updateDto, existingPhongBan);
            
            var updated = await _repository.UpdateAsync(existingPhongBan);
            return _mapper.Map<PhongBanDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
