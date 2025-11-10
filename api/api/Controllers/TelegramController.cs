using api.Data;
using api.DTO;
using api.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu đăng nhập
    public class TelegramController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramController> _logger;

        public TelegramController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<TelegramController> logger)
        {
            _context = context;
            _configuration = configuration;
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
                // Lấy NhanVienId từ JWT token (sử dụng BaseApiController)
                var nhanVienId = GetCurrentUserId();

                // Tìm nhân viên
                var nhanVien = await _context.NhanViens
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(n => n.Id == nhanVienId);

                if (nhanVien == null)
                {
                    return NotFound("Không tìm thấy thông tin nhân viên");
                }

                // Kiểm tra đã liên kết chưa
                if (!string.IsNullOrEmpty(nhanVien.TelegramChatId))
                {
                    return BadRequest(new 
                    { 
                        message = "Tài khoản đã được liên kết với Telegram",
                        isLinked = true,
                        chatId = nhanVien.TelegramChatId
                    });
                }

                // Xóa các token cũ chưa dùng của user này
                var oldTokens = await _context.TelegramLinkTokens
                    .Where(t => t.NhanVienId == nhanVien.Id && !t.IsUsed)
                    .ToListAsync();
                _context.TelegramLinkTokens.RemoveRange(oldTokens);

                // Tạo token mới
                var token = Guid.NewGuid().ToString("N"); // 32 ký tự không có dấu gạch ngang
                var expiryMinutes = _configuration.GetValue<int>("Telegram:LinkTokenExpiryMinutes", 10);
                var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

                var linkToken = new TelegramLinkToken
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    NhanVienId = nhanVien.Id,
                    ExpiresAt = expiresAt,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.TelegramLinkTokens.Add(linkToken);
                await _context.SaveChangesAsync();

                // Tạo deep link
                var botUsername = _configuration["Telegram:BotUsername"] ?? "company_manager_sp_bot";
                var deepLink = $"https://t.me/{botUsername}?start={token}";

                var response = new TelegramLinkResponseDto
                {
                    DeepLink = deepLink,
                    Token = token,
                    ExpiresAt = expiresAt,
                    ExpiresInSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds
                };

                _logger.LogInformation($"Đã tạo deep link cho nhân viên {nhanVien.TenDayDu}");

                return Ok(response);
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

                var nhanVien = await _context.NhanViens
                    .FirstOrDefaultAsync(n => n.Id == nhanVienId);

                if (nhanVien == null)
                {
                    return NotFound("Không tìm thấy thông tin nhân viên");
                }

                var isLinked = !string.IsNullOrEmpty(nhanVien.TelegramChatId);

                // Tìm token đang chờ (nếu có)
                var pendingToken = await _context.TelegramLinkTokens
                    .Where(t => t.NhanVienId == nhanVien.Id && 
                               !t.IsUsed && 
                               t.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    isLinked = isLinked,
                    chatId = nhanVien.TelegramChatId,
                    hasPendingToken = pendingToken != null,
                    pendingTokenExpiresAt = pendingToken?.ExpiresAt,
                    pendingTokenExpiresInSeconds = pendingToken != null 
                        ? (int)(pendingToken.ExpiresAt - DateTime.UtcNow).TotalSeconds 
                        : 0
                });
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

                var nhanVien = await _context.NhanViens
                    .FirstOrDefaultAsync(n => n.Id == nhanVienId);

                if (nhanVien == null)
                {
                    return NotFound("Không tìm thấy thông tin nhân viên");
                }

                if (string.IsNullOrEmpty(nhanVien.TelegramChatId))
                {
                    return BadRequest("Tài khoản chưa được liên kết với Telegram");
                }

                // Xóa ChatId
                nhanVien.TelegramChatId = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã hủy liên kết Telegram cho nhân viên {nhanVien.TenDayDu}");

                return Ok(new { message = "Đã hủy liên kết Telegram thành công" });
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
                var linkToken = await _context.TelegramLinkTokens
                    .Include(t => t.NhanVien)
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (linkToken == null)
                {
                    return NotFound(new { valid = false, message = "Token không tồn tại" });
                }

                if (linkToken.IsUsed)
                {
                    return Ok(new 
                    { 
                        valid = false, 
                        message = "Token đã được sử dụng",
                        usedAt = linkToken.UsedAt
                    });
                }

                if (linkToken.ExpiresAt < DateTime.UtcNow)
                {
                    return Ok(new 
                    { 
                        valid = false, 
                        message = "Token đã hết hạn",
                        expiresAt = linkToken.ExpiresAt
                    });
                }

                return Ok(new
                {
                    valid = true,
                    message = "Token hợp lệ",
                    nhanVien = linkToken.NhanVien?.TenDayDu,
                    expiresAt = linkToken.ExpiresAt,
                    expiresInSeconds = (int)(linkToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi verify token");
                return StatusCode(500, "Có lỗi xảy ra");
            }
        }
    }
}
