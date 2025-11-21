using api.DTO;
using api.Model.Enums;
using api.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class NghiPhepQuotaController : BaseApiController
    {
        private readonly INghiPhepQuotaService _quotaService;

        public NghiPhepQuotaController(INghiPhepQuotaService quotaService)
        {
            _quotaService = quotaService;
        }

        /// <summary>
        /// Lấy dashboard "Lịch Nghỉ & Công Việc" của nhân viên hiện tại
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<LichNghiDashboardDto>> GetMyDashboard(
            [FromQuery] int? nam = null,
            [FromQuery] int? thang = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var dashboard = await _quotaService.GetLichNghiDashboardAsync(currentUserId, nam, thang);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi khi lấy dashboard", ex.Message);
            }
        }

        /// <summary>
        /// Lấy quota tháng hiện tại của nhân viên
        /// </summary>
        [HttpGet("my-quota")]
        public async Task<ActionResult<NghiPhepQuotaDto>> GetMyQuota(
            [FromQuery] int? nam = null,
            [FromQuery] int? thang = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var now = DateTime.UtcNow;
                var targetNam = nam ?? now.Year;
                var targetThang = thang ?? now.Month;

                var quota = await _quotaService.GetOrCreateQuotaAsync(currentUserId, targetNam, targetThang);
                return Ok(quota);
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi", ex.Message);
            }
        }

        /// <summary>
        /// Lấy calendar view của tháng
        /// </summary>
        [HttpGet("calendar")]
        public async Task<ActionResult<LichNghiCalendarDto>> GetCalendar(
            [FromQuery] int nam,
            [FromQuery] int thang)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest("Tháng không hợp lệ");

                var currentUserId = GetCurrentUserId();
                var calendar = await _quotaService.GetCalendarAsync(currentUserId, nam, thang);
                return Ok(calendar);
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi", ex.Message);
            }
        }

        #region Admin Operations (Giám Đốc)

        /// <summary>
        /// Lấy danh sách quota của tất cả nhân viên trong tháng (Giám Đốc)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult<List<NghiPhepQuotaDto>>> GetAllQuotas(
            [FromQuery] int nam,
            [FromQuery] int thang,
            [FromQuery] Guid? phongBanId = null)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest("Tháng không hợp lệ");

                var quotas = await _quotaService.GetQuotasByMonthAsync(nam, thang, phongBanId);
                return Ok(quotas);
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi", ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật quota (Giám Đốc thay đổi số ngày phép)
        /// </summary>
        [HttpPut("{quotaId}")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult<NghiPhepQuotaDto>> UpdateQuota(
            Guid quotaId,
            [FromBody] UpsertNghiPhepQuotaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var quota = await _quotaService.UpdateQuotaAsync(quotaId, dto);
                return Ok(quota);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi", ex.Message);
            }
        }

        /// <summary>
        /// Recalculate quota (dùng khi cần sync lại dữ liệu)
        /// </summary>
        [HttpPost("recalculate")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult> RecalculateQuota(
            [FromQuery] Guid nhanVienId,
            [FromQuery] int nam,
            [FromQuery] int thang)
        {
            try
            {
                await _quotaService.RecalculateQuotaAsync(nhanVienId, nam, thang);
                return Ok(new { message = "Đã tính lại quota thành công" });
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi", ex.Message);
            }
        }

        /// <summary>
        /// Bulk create/update quota cho nhiều nhân viên cùng lúc (Giám Đốc)
        /// Use case: Đầu tháng tạo quota cho toàn bộ nhân viên, hoặc cập nhật hàng loạt
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Roles = AppRolesExtensions.GiamDoc)]
        public async Task<ActionResult<BulkQuotaResultDto>> BulkCreateOrUpdateQuota([FromBody] BulkQuotaRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request.Thang < 1 || request.Thang > 12)
                    return BadRequest("Tháng không hợp lệ");

                var result = await _quotaService.BulkCreateOrUpdateQuotaAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError("Đã xảy ra lỗi khi cấu hình hàng loạt", ex.Message);
            }
        }


        #endregion
    }

    /// <summary>
    /// Request DTO cho validate quota
    /// </summary>
    public class ValidateQuotaRequest
    {
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public decimal SoNgayNghi { get; set; }
    }
}
