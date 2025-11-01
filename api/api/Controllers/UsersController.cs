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
                    return BadRequest(ModelState);

                var response = await _authService.LoginAsync(dto);
                if (response == null)
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đăng nhập", error = ex.Message });
            }
        }

        /// <summary>
        /// Đăng ký user mới (tạo cả User và NhanVien)
        /// Chỉ Giám Đốc và Phó Giám Đốc mới được tạo user
        /// </summary>
        [HttpPost("register")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrPhoGiamDoc)]
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
        /// Chỉ Giám Đốc và Phó Giám Đốc mới được update
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrPhoGiamDoc)]
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
    }
}
