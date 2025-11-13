using api.DTO;
using api.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu đăng nhập
    public class TelegramController : BaseApiController
    {
        private readonly ITelegramLinkService _telegramLinkService;
        private readonly ILogger<TelegramController> _logger;

        public TelegramController(
            ITelegramLinkService telegramLinkService,
            ILogger<TelegramController> logger)
        {
            _telegramLinkService = telegramLinkService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo deep link để liên kết Telegram
        /// </summary>
        /// <returns>Deep link và thông tin token</returns>
        [HttpPost("generate-link")]
        public async Task<ActionResult<TelegramLinkResponseDto>> GenerateDeepLink()
        {
            try
            {
                var nhanVienId = GetCurrentUserId();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var response = await _telegramLinkService.GenerateDeepLinkAsync(nhanVienId, ipAddress);

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    isLinked = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo deep link");
                return StatusCode(500, "Có lỗi xảy ra khi tạo link liên kết");
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái liên kết Telegram
        /// </summary>
        /// <returns>Thông tin trạng thái</returns>
        [HttpGet("link-status")]
        public async Task<ActionResult> GetLinkStatus()
        {
            try
            {
                var nhanVienId = GetCurrentUserId();
                var status = await _telegramLinkService.GetLinkStatusAsync(nhanVienId);

                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra trạng thái liên kết");
                return StatusCode(500, "Có lỗi xảy ra");
            }
        }

        /// <summary>
        /// Hủy liên kết Telegram (unlink)
        /// </summary>
        /// <returns></returns>
        [HttpPost("unlink")]
        public async Task<ActionResult> UnlinkTelegram()
        {
            try
            {
                var nhanVienId = GetCurrentUserId();
                await _telegramLinkService.UnlinkTelegramAsync(nhanVienId);

                return Ok(new { message = "Đã hủy liên kết Telegram thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy liên kết");
                return StatusCode(500, "Có lỗi xảy ra");
            }
        }

        /// <summary>
        /// Kiểm tra token có hợp lệ không (for testing)
        /// </summary>
        [HttpGet("verify-token/{token}")]
        public async Task<ActionResult> VerifyToken(string token)
        {
            try
            {
                var result = await _telegramLinkService.VerifyTokenAsync(token);

                if (!result.Valid)
                {
                    return Ok(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi verify token");
                return StatusCode(500, "Có lỗi xảy ra");
            }
        }
    }
}
