using api.DTO;
using api.Model.Enums;
using api.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [Authorize] // Tất cả endpoints đều cần đăng nhập
    public class DonYeuCausController : BaseApiController
    {
        private readonly IDonYeuCauService _donYeuCauService;

        public DonYeuCausController(IDonYeuCauService donYeuCauService)
        {
            _donYeuCauService = donYeuCauService;
        }

        #region CRUD Operations

        /// <summary>
        /// Lấy danh sách tất cả đơn yêu cầu với filter (Giám Đốc và Trưởng Phòng)
        /// - Giám Đốc: Xem tất cả đơn toàn công ty
        /// - Trưởng Phòng: Tự động filter theo phòng ban của mình
        /// </summary>
        [HttpGet]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<PagedResult<DonYeuCauDto>>> GetAll([FromQuery] FilterDonYeuCauDto filter)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _donYeuCauService.GetAllAsync(filter, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách đơn", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách đơn ĐÃ XỬ LÝ (Giám Đốc - Audit/Report)
        /// Chỉ trả về: DaChapThuan, BiTuChoi, DaHuy
        /// Không bao gồm: DangChoDuyet (để Trưởng Phòng xử lý)
        /// </summary>
        [HttpGet("processed")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult<PagedResult<DonYeuCauDto>>> GetProcessedDons([FromQuery] FilterDonYeuCauDto filter)
        {
            try
            {
                var result = await _donYeuCauService.GetProcessedDonsAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách đơn đã xử lý", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy đơn yêu cầu theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DonYeuCauDto>> GetById(Guid id)
        {
            try
            {
                var don = await _donYeuCauService.GetByIdAsync(id);
                if (don == null)
                    return NotFound(new { message = $"Không tìm thấy đơn với ID: {id}" });

                // Kiểm tra quyền xem: owner hoặc Giám Đốc/Trưởng Phòng
                var currentUserId = GetCurrentUserId();
                if (don.NhanVienId != currentUserId && !IsGiamDocOrTruongPhong())
                    return Forbid();

                return Ok(don);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo đơn yêu cầu mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DonYeuCauDto>> Create([FromBody] CreateDonYeuCauDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var don = await _donYeuCauService.CreateAsync(currentUserId, dto);

                return CreatedAtAction(nameof(GetById), new { id = don.Id }, don);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo đơn", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật đơn yêu cầu (chỉ owner và đơn đang chờ duyệt)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DonYeuCauDto>> Update(Guid id, [FromBody] UpdateDonYeuCauDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var updated = await _donYeuCauService.UpdateAsync(id, currentUserId, dto);

                if (updated == null)
                    return NotFound(new { message = $"Không tìm thấy đơn với ID: {id}" });

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật đơn", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa đơn yêu cầu (Giám Đốc hoặc owner khi đơn chưa duyệt)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isGiamDoc = IsGiamDoc();

                var result = await _donYeuCauService.DeleteAsync(id, currentUserId, isGiamDoc);

                if (!result)
                    return NotFound(new { message = $"Không tìm thấy đơn với ID: {id}" });

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa đơn", error = ex.Message });
            }
        }

        #endregion

        #region My Dons (Đơn của tôi)

        /// <summary>
        /// Lấy danh sách đơn của tôi
        /// </summary>
        [HttpGet("my-dons")]
        public async Task<ActionResult<PagedResult<DonYeuCauDto>>> GetMyDons(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? maDon = null,
            [FromQuery] string? lyDo = null,
            [FromQuery] LoaiDonYeuCau? loaiDon = null,
            [FromQuery] TrangThaiDon? trangThai = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _donYeuCauService.GetMyDonsAsync(
                    currentUserId, pageNumber, pageSize, maDon, lyDo, loaiDon, trangThai);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Hủy đơn của tôi (chỉ đơn đang chờ duyệt)
        /// </summary>
        [HttpPost("{id}/huy")]
        public async Task<ActionResult> HuyDon(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _donYeuCauService.HuyDonAsync(id, currentUserId);

                if (!result)
                    return BadRequest(new { message = "Không thể hủy đơn này" });

                return Ok(new { message = "Đã hủy đơn thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        #endregion

        #region Duyệt Đơn (Giám Đốc và Trưởng Phòng)

        /// <summary>
        /// Lấy danh sách đơn cần duyệt (Giám Đốc và Trưởng Phòng)
        /// </summary>
        [HttpGet("can-duyet")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<PagedResult<DonYeuCauDto>>> GetDonCanDuyet(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _donYeuCauService.GetDonCanDuyetAsync(
                    currentUserId, pageNumber, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Chấp thuận đơn (Giám Đốc và Trưởng Phòng)
        /// </summary>
        [HttpPost("{id}/chap-thuan")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<DonYeuCauDto>> ChapThuan(Guid id, [FromBody] DuyetDonYeuCauDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var don = await _donYeuCauService.ChapThuanDonAsync(
                    id, currentUserId, dto.GhiChuNguoiDuyet);

                return Ok(don);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Từ chối đơn (Giám Đốc và Trưởng Phòng)
        /// </summary>
        [HttpPost("{id}/tu-choi")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<DonYeuCauDto>> TuChoi(Guid id, [FromBody] DuyetDonYeuCauDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.GhiChuNguoiDuyet))
                    return BadRequest(new { message = "Vui lòng nhập lý do từ chối" });

                var currentUserId = GetCurrentUserId();
                var don = await _donYeuCauService.TuChoiDonAsync(
                    id, currentUserId, dto.GhiChuNguoiDuyet);

                return Ok(don);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Đếm số đơn đang chờ duyệt (Giám Đốc và Trưởng Phòng)
        /// </summary>
        [HttpGet("count-cho-duyet")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<int>> CountDonChoDuyet()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var count = await _donYeuCauService.CountDonChoDuyetAsync(currentUserId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách ngày đã nghỉ phép (đã được chấp thuận)
        /// Dùng để highlight trên datepicker
        /// </summary>
        [HttpGet("ngay-da-nghi")]
        public async Task<ActionResult<List<DateTime>>> GetNgayDaNghi(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var dates = await _donYeuCauService.GetNgayDaNghiAsync(currentUserId, fromDate, toDate);
                return Ok(dates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        #endregion

        #region Thống kê

        /// <summary>
        /// Thống kê đơn của tôi
        /// </summary>
        [HttpGet("thong-ke/my-dons")]
        public async Task<ActionResult<ThongKeDonYeuCauDto>> ThongKeMyDons(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _donYeuCauService.ThongKeMyDonsAsync(currentUserId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê đơn theo phòng ban (Trưởng Phòng)
        /// </summary>
        [HttpGet("thong-ke/phong-ban/{phongBanId}")]
        [Authorize(Roles = AppRolesExtensions.GiamDocOrTruongPhong)]
        public async Task<ActionResult<ThongKeDonYeuCauDto>> ThongKePhongBan(
            Guid phongBanId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var result = await _donYeuCauService.ThongKePhongBanAsync(phongBanId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê đơn toàn công ty (chỉ Giám Đốc)
        /// </summary>
        [HttpGet("thong-ke/toan-cong-ty")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult<ThongKeDonYeuCauDto>> ThongKeToanCongTy(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var result = await _donYeuCauService.ThongKeToanCongTyAsync(fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi", error = ex.Message });
            }
        }

        #endregion
    }
}

