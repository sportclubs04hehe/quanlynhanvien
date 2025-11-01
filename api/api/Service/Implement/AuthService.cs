using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace api.Service.Implement
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly INhanVienRepository _nhanVienRepo;
        private readonly IMapper _mapper;

        public AuthService(
            UserManager<User> userManager,
            INhanVienRepository nhanVienRepo,
            IMapper mapper)
        {
            _userManager = userManager;
            _nhanVienRepo = nhanVienRepo;
            _mapper = mapper;
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
        {
            // 1. Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email đã tồn tại trong hệ thống");
            }

            // 2. Tạo User qua Identity
            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true // Tự động confirm cho dễ test
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Không thể tạo user: {errors}");
            }

            try
            {
                // 3. Tạo NhanVien với cùng ID
                var nhanVien = _mapper.Map<NhanVien>(dto);
                nhanVien.Id = user.Id; // ← QUAN TRỌNG: Cùng ID với User
                nhanVien.NgayVaoLam = dto.NgayVaoLam ?? DateTime.UtcNow;
                nhanVien.Status = NhanVienStatus.Active;

                await _nhanVienRepo.CreateAsync(nhanVien);

                // 4. Load lại để có đầy đủ navigation properties
                var createdNhanVien = await _nhanVienRepo.GetByIdAsync(user.Id);
                if (createdNhanVien == null)
                {
                    throw new InvalidOperationException("Không thể tải thông tin nhân viên sau khi tạo");
                }

                // 5. Trả về DTO
                return _mapper.Map<UserDto>(createdNhanVien);
            }
            catch
            {
                // Rollback: Xóa User nếu tạo NhanVien thất bại
                await _userManager.DeleteAsync(user);
                throw;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var nhanVien = await _nhanVienRepo.GetByIdAsync(id);
            return nhanVien == null ? null : _mapper.Map<UserDto>(nhanVien);
        }

        public async Task<PagedResult<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var (items, totalCount) = await _nhanVienRepo.GetAllAsync(pageNumber, pageSize, searchTerm);

            var dtos = _mapper.Map<List<UserDto>>(items);

            return new PagedResult<UserDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            // 1. Lấy NhanVien hiện tại
            var existingNhanVien = await _nhanVienRepo.GetByIdAsync(id);
            if (existingNhanVien == null)
                return null;

            // 2. Update thông tin NhanVien
            _mapper.Map(dto, existingNhanVien);

            // 3. Update PhoneNumber trong User
            if (dto.PhoneNumber != existingNhanVien.User.PhoneNumber)
            {
                existingNhanVien.User.PhoneNumber = dto.PhoneNumber;
                await _userManager.UpdateAsync(existingNhanVien.User);
            }

            // 4. Save NhanVien
            var updated = await _nhanVienRepo.UpdateAsync(existingNhanVien);

            // 5. Load lại để có đầy đủ navigation properties
            var result = await _nhanVienRepo.GetByIdAsync(id);
            return result == null ? null : _mapper.Map<UserDto>(result);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            // 1. Lấy NhanVien
            var nhanVien = await _nhanVienRepo.GetByIdAsync(id);
            if (nhanVien == null)
                return false;

            // 2. Xóa NhanVien trước (vì có FK constraint)
            var nhanVienDeleted = await _nhanVienRepo.DeleteAsync(id);
            if (!nhanVienDeleted)
                return false;

            // 3. Xóa User
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return true;
        }
    }
}
