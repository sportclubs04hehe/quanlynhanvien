using api.Data;
using api.DTO;
using api.Model;
using api.Model.Enums;
using api.Repository.Interface;
using api.Service.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            INhanVienRepository nhanVienRepo,
            IMapper mapper,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _nhanVienRepo = nhanVienRepo;
            _mapper = mapper;
            _configuration = configuration;
            _context = context;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto, string? ipAddress = null)
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

            // 6. Tạo Refresh Token
            var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

            // 7. Cleanup: Xóa expired tokens + giới hạn số tokens active
            await CleanupAndLimitTokensAsync(user.Id);

            // 8. Tạo UserDto
            var userDto = _mapper.Map<UserDto>(nhanVien);
            userDto.Roles = roles.ToList();

            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            return new LoginResponseDto
            {
                TokenType = "Bearer",
                AccessToken = token,
                ExpiresIn = expiryMinutes * 60, // Convert to seconds
                RefreshToken = refreshToken.Token,
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
                    role != AppRolesExtensions.TruongPhong && 
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

            // 4. Update Role (chỉ nếu dto.Role được cung cấp và hợp lệ)
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                // Validate role hợp lệ
                if (dto.Role == AppRolesExtensions.GiamDoc || 
                    dto.Role == AppRolesExtensions.TruongPhong || 
                    dto.Role == AppRolesExtensions.NhanVien)
                {
                    var currentRoles = await _userManager.GetRolesAsync(existingNhanVien.User);
                    
                    // Chỉ update nếu role khác với role hiện tại
                    if (!currentRoles.Contains(dto.Role))
                    {
                        // Xóa tất cả roles hiện tại
                        if (currentRoles.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(existingNhanVien.User, currentRoles);
                        }
                        
                        // Thêm role mới
                        await _userManager.AddToRoleAsync(existingNhanVien.User, dto.Role);
                    }
                }
            }

            // 5. Save NhanVien
            var updated = await _nhanVienRepo.UpdateAsync(existingNhanVien);

            // 6. Load lại để có đầy đủ navigation properties
            var result = await _nhanVienRepo.GetByIdAsync(id);
            if (result == null) return null;
            
            // 7. Load roles và map vào DTO
            var userDto = _mapper.Map<UserDto>(result);
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            }
            
            return userDto;
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

        #region Refresh Token Methods

        public async Task<LoginResponseDto?> RefreshTokenAsync(string accessToken, string refreshToken, string? ipAddress = null)
        {
            // 1. Validate refresh token - không dùng IsActive trong query
            var now = DateTime.UtcNow;
            var storedRefreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            // Check IsActive sau khi query (in-memory)
            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
                return null;

            // 2. Lấy thông tin user từ expired access token (không validate expiration)
            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
                return null;

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            
            // 3. Kiểm tra user ID trong token khớp với refresh token
            if (storedRefreshToken.UserId != userId)
                return null;

            // 4. Lấy thông tin NhanVien
            var nhanVien = await _nhanVienRepo.GetByIdAsync(userId);
            if (nhanVien == null)
                return null;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return null;

            // 5. Revoke old refresh token và tạo mới
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            storedRefreshToken.RevokedByIp = ipAddress;

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles.ToList());
            var newRefreshToken = await GenerateRefreshTokenAsync(userId, ipAddress);

            storedRefreshToken.ReplacedByToken = newRefreshToken.Token;
            await _context.SaveChangesAsync();

            // 6. Tạo response
            var userDto = _mapper.Map<UserDto>(nhanVien);
            userDto.Roles = roles.ToList();

            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            return new LoginResponseDto
            {
                TokenType = "Bearer",
                AccessToken = newAccessToken,
                ExpiresIn = expiryMinutes * 60,
                RefreshToken = newRefreshToken.Token,
                User = userDto
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || !storedToken.IsActive)
                return false;

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Revoke token theo ID (dùng cho revoke single session từ UI)
        /// Chỉ cho phép user revoke token của chính mình
        /// </summary>
        public async Task<bool> RevokeTokenByIdAsync(Guid tokenId, Guid userId, string? ipAddress = null)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == tokenId && rt.UserId == userId);

            if (storedToken == null || !storedToken.IsActive)
                return false;

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Revoke TẤT CẢ tokens của user
        /// Dùng khi: Đổi mật khẩu, phát hiện bất thường, admin force logout
        /// </summary>
        public async Task<int> RevokeAllUserTokensAsync(Guid userId, string? ipAddress = null)
        {
            var now = DateTime.UtcNow;
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            !rt.IsRevoked && 
                            rt.ExpiresAt >= now)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
            }

            await _context.SaveChangesAsync();
            return activeTokens.Count;
        }

        /// <summary>
        /// Lấy danh sách sessions đang active (devices đang login)
        /// </summary>
        public async Task<List<RefreshTokenInfoDto>> GetActiveSessionsAsync(Guid userId, string? currentRefreshToken = null)
        {
            var now = DateTime.UtcNow;
            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            !rt.IsRevoked && 
                            rt.ExpiresAt >= now)
                .OrderByDescending(rt => rt.CreatedAt)
                .Select(rt => new RefreshTokenInfoDto
                {
                    Id = rt.Id,
                    Token = "..." + rt.Token.Substring(Math.Max(0, rt.Token.Length - 10)), // Chỉ hiển thị 10 ký tự cuối
                    CreatedAt = rt.CreatedAt,
                    ExpiresAt = rt.ExpiresAt,
                    CreatedByIp = rt.CreatedByIp,
                    IsActive = true,
                    IsCurrentSession = !string.IsNullOrEmpty(currentRefreshToken) && rt.Token == currentRefreshToken
                })
                .ToListAsync();

            return activeSessions;
        }

        public async Task CleanupExpiredTokensAsync(Guid userId)
        {
            // Không dùng computed property IsActive trong query
            // Thay vào đó query trực tiếp các điều kiện
            var now = DateTime.UtcNow;
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            (rt.IsRevoked || rt.ExpiresAt < now))
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cleanup expired tokens + Giới hạn số lượng active tokens
        /// Giữ tối đa 5 active tokens (cho 5 devices khác nhau)
        /// </summary>
        private async Task CleanupAndLimitTokensAsync(Guid userId, int maxActiveTokens = 5)
        {
            // 1. Xóa expired và revoked tokens
            await CleanupExpiredTokensAsync(userId);

            // 2. Lấy danh sách active tokens, sắp xếp theo ngày tạo mới nhất
            var now = DateTime.UtcNow;
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            !rt.IsRevoked && 
                            rt.ExpiresAt >= now)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();

            // 3. Nếu vượt quá giới hạn, revoke các tokens cũ nhất
            if (activeTokens.Count > maxActiveTokens)
            {
                var tokensToRevoke = activeTokens.Skip(maxActiveTokens).ToList();
                foreach (var token in tokensToRevoke)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    // Không cần RevokedByIp vì đây là auto cleanup
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress = null)
        {
            var now = DateTime.UtcNow;
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureRandomToken(),
                UserId = userId,
                CreatedAt = now, // Explicitly set CreatedAt
                ExpiresAt = now.AddDays(7), // Refresh token có hiệu lực 7 ngày
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        private string GenerateSecureRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Change Password

        public async Task<ChangePasswordResponseDto> ChangePasswordAsync(
            Guid userId, 
            ChangePasswordDto dto, 
            string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng"
                };
            }

            // Kiểm tra mật khẩu hiện tại
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isPasswordCorrect)
            {
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu hiện tại không đúng"
                };
            }

            // Đổi mật khẩu
            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = $"Đổi mật khẩu thất bại: {errors}"
                };
            }

            // Revoke tất cả refresh tokens cũ để bắt người dùng phải đăng nhập lại
            await RevokeAllUserTokensAsync(userId, ipAddress);

            return new ChangePasswordResponseDto
            {
                Success = true,
                Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
            };
        }

        #endregion
    }
}
