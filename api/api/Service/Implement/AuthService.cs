using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api.Service.Implement
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly INhanVienRepository _nhanVienRepo;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            INhanVienRepository nhanVienRepo,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _nhanVienRepo = nhanVienRepo;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            // 1. Tìm user theo email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return null;

            // 2. Kiểm tra password
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return null;

            // 3. Lấy thông tin NhanVien
            var nhanVien = await _nhanVienRepo.GetByIdAsync(user.Id);
            if (nhanVien == null)
                return null;

            // 4. Load roles
            var roles = await _userManager.GetRolesAsync(user);

            // 5. Tạo JWT token
            var token = GenerateJwtToken(user, roles.ToList());

            // 6. Tạo UserDto
            var userDto = _mapper.Map<UserDto>(nhanVien);
            userDto.Roles = roles.ToList();

            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            return new LoginResponseDto
            {
                TokenType = "Bearer",
                AccessToken = token,
                ExpiresIn = expiryMinutes * 60, // Convert to seconds
                RefreshToken = "", // Có thể implement refresh token sau
                User = userDto
            };
        }

        private string GenerateJwtToken(User user, List<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Add roles as claims
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Không thể tạo user: {errors}");
            }

            try
            {
                // 3. Gán Role (mặc định là NhanVien nếu không chọn)
                var role = string.IsNullOrWhiteSpace(dto.Role) 
                    ? AppRolesExtensions.NhanVien 
                    : dto.Role;
                
                // Validate role có hợp lệ không
                if (role != AppRolesExtensions.GiamDoc && 
                    role != AppRolesExtensions.PhoGiamDoc && 
                    role != AppRolesExtensions.NhanVien)
                {
                    role = AppRolesExtensions.NhanVien; // Fallback
                }
                
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Không thể gán role: {roleErrors}");
                }

                // 4. Tạo NhanVien với cùng ID
                var nhanVien = _mapper.Map<NhanVien>(dto);
                nhanVien.Id = user.Id; // ← QUAN TRỌNG: Cùng ID với User
                nhanVien.NgayVaoLam = dto.NgayVaoLam ?? DateTime.UtcNow;
                nhanVien.Status = NhanVienStatus.Active;

                await _nhanVienRepo.CreateAsync(nhanVien);

                // 5. Load lại để có đầy đủ navigation properties
                var createdNhanVien = await _nhanVienRepo.GetByIdAsync(user.Id);
                if (createdNhanVien == null)
                {
                    throw new InvalidOperationException("Không thể tải thông tin nhân viên sau khi tạo");
                }

                // 6. Trả về DTO (bao gồm roles)
                var userDto = _mapper.Map<UserDto>(createdNhanVien);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                
                return userDto;
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
            if (nhanVien == null) return null;
            
            var userDto = _mapper.Map<UserDto>(nhanVien);
            
            // Load roles
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            }
            
            return userDto;
        }

        public async Task<PagedResult<UserDto>> GetAllUsersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var (items, totalCount) = await _nhanVienRepo.GetAllAsync(pageNumber, pageSize, searchTerm);

            var dtos = _mapper.Map<List<UserDto>>(items);
            
            // Load roles cho từng user
            foreach (var dto in dtos)
            {
                var user = await _userManager.FindByIdAsync(dto.Id.ToString());
                if (user != null)
                {
                    dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                }
            }

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
