using api.DTO;
using api.Service.Interface;
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
        /// Đăng ký user mới (tạo cả User và NhanVien)
        /// </summary>
        [HttpPost("register")]
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
        /// </summary>
        [HttpGet]
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
        /// </summary>
        [HttpPut("{id}")]
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
        /// </summary>
        [HttpDelete("{id}")]
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
