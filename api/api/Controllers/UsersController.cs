using api.DTO;
using api.Model.Enums;
using api.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var response = await _authService.LoginAsync(dto, ipAddress);
                
                if (response == null)
                    return Unauthorized(new { success = false, message = "Email hoặc mật khẩu không đúng" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đăng nhập", error = ex.Message });
            }
        }

        /// <summary>
        /// Đăng ký user mới (tạo cả User và NhanVien)
        /// Chỉ Giám Đốc và Trưởng Phòng mới được tạo user
        /// </summary>
        [HttpPost("register")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _authService.RegisterAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo user", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách users với phân trang và tìm kiếm
        /// Tất cả users đã đăng nhập đều xem được
        /// </summary>
        [HttpGet]
        [Authorize(Roles = AppRolesExtensions.AllRoles)]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("PageNumber và PageSize phải lớn hơn 0");

            var result = await _authService.GetAllUsersAsync(pageNumber, pageSize, searchTerm);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết user theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound($"Không tìm thấy user với ID: {id}");

            return Ok(user);
        }

        /// <summary>
        /// Cập nhật thông tin user
        /// Chỉ Giám Đốc và Trưởng Phòng mới được update
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updated = await _authService.UpdateUserAsync(id, dto);
                if (updated == null)
                    return NotFound($"Không tìm thấy user với ID: {id}");

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật user", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa user (xóa cả User và NhanVien)
        /// CHỈ Giám Đốc mới được xóa
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _authService.DeleteUserAsync(id);
                if (!result)
                    return NotFound($"Không tìm thấy user với ID: {id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa user", error = ex.Message });
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var response = await _authService.RefreshTokenAsync(dto.AccessToken, dto.RefreshToken, ipAddress);
                
                if (response == null)
                    return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi refresh token", error = ex.Message });
            }
        }

        /// <summary>
        /// Revoke refresh token
        /// </summary>
        [HttpPost("revoke-token")]
        [AllowAnonymous] // Cho phép revoke khi logout (có thể chưa có token)
        public async Task<ActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.RevokeTokenAsync(dto.RefreshToken, ipAddress);
                
                if (!result)
                    return BadRequest(new { message = "Refresh token không hợp lệ" });

                return Ok(new { message = "Token đã được thu hồi thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi revoke token", error = ex.Message });
            }
        }

        /// <summary>
        /// Revoke TẤT CẢ refresh tokens của user hiện tại
        /// Dùng khi: Đổi mật khẩu, nghi ngờ bị hack
        /// </summary>
        [HttpPost("revoke-all-tokens")]
        [Authorize]
        public async Task<ActionResult> RevokeAllTokens()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var revokedCount = await _authService.RevokeAllUserTokensAsync(userId, ipAddress);
                
                return Ok(new { 
                    message = $"Đã thu hồi {revokedCount} tokens thành công",
                    revokedCount 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi revoke all tokens", error = ex.Message });
            }
        }

        /// <summary>
        /// Xem danh sách devices đang login (active sessions)
        /// </summary>
        [HttpGet("active-sessions")]
        [Authorize]
        public async Task<ActionResult<List<RefreshTokenInfoDto>>> GetActiveSessions()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
                
                // Lấy current refresh token từ request header hoặc body (nếu có)
                // Để identify current session
                var currentRefreshToken = Request.Headers["X-Refresh-Token"].FirstOrDefault();
                
                var sessions = await _authService.GetActiveSessionsAsync(userId, currentRefreshToken);
                
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách sessions", error = ex.Message });
            }
        }

        /// <summary>
        /// Revoke một session cụ thể (đăng xuất thiết bị khác)
        /// </summary>
        [HttpPost("revoke-session/{tokenId}")]
        [Authorize]
        public async Task<ActionResult> RevokeSession(Guid tokenId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                var result = await _authService.RevokeTokenByIdAsync(tokenId, userId, ipAddress);
                
                if (!result)
                    return BadRequest(new { message = "Token không hợp lệ hoặc không thuộc về bạn" });

                return Ok(new { message = "Đã đăng xuất thiết bị thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi revoke session", error = ex.Message });
            }
        }
    }
}
