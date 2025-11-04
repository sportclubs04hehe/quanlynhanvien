using api.DTO;
using api.Model.Enums;
using api.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
    public class PhongBansController : BaseApiController
    {
        private readonly IPhongBanService _service;

        public PhongBansController(IPhongBanService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách phòng ban với phân trang và tìm kiếm
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<PhongBanDto>>> GetAll(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1 || pageSize < 1)
                return BadRequest("PageNumber và PageSize phải lớn hơn 0");

            var result = await _service.GetAllAsync(pageNumber, pageSize, searchTerm);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết phòng ban theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PhongBanDto>> GetById(Guid id)
        {
            var phongBan = await _service.GetByIdAsync(id);
            if (phongBan == null)
                return NotFound($"Không tìm thấy phòng ban với ID: {id}");

            return Ok(phongBan);
        }

        /// <summary>
        /// Tạo phòng ban mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PhongBanDto>> Create([FromBody] CreatePhongBanDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Cập nhật thông tin phòng ban
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PhongBanDto>> Update(Guid id, [FromBody] UpdatePhongBanDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, updateDto);
            if (updated == null)
                return NotFound($"Không tìm thấy phòng ban với ID: {id}");

            return Ok(updated);
        }

        /// <summary>
        /// Xóa phòng ban
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound($"Không tìm thấy phòng ban với ID: {id}");

            return NoContent();
        }
    }
}
