using api.DTO;
using api.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChucVusController : ControllerBase
    {
        private readonly IChucVuService _service;

        public ChucVusController(IChucVuService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách chức vụ với phân trang và tìm kiếm
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<ChucVuDto>>> GetAll(
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
        /// Lấy thông tin chi tiết chức vụ theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChucVuDto>> GetById(Guid id)
        {
            var chucVu = await _service.GetByIdAsync(id);
            if (chucVu == null)
                return NotFound($"Không tìm thấy chức vụ với ID: {id}");

            return Ok(chucVu);
        }

        /// <summary>
        /// Tạo chức vụ mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ChucVuDto>> Create([FromBody] CreateChucVuDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Cập nhật thông tin chức vụ
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ChucVuDto>> Update(Guid id, [FromBody] UpdateChucVuDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, updateDto);
            if (updated == null)
                return NotFound($"Không tìm thấy chức vụ với ID: {id}");

            return Ok(updated);
        }

        /// <summary>
        /// Xóa chức vụ
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound($"Không tìm thấy chức vụ với ID: {id}");

            return NoContent();
        }
    }
}
