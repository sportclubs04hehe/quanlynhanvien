using api.DTO;
using api.Model;
using api.Repository.Interface;
using api.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace api.Service.Implement
{
    /// <summary>
    /// Service triển khai business logic liên kết Telegram
    /// </summary>
    public class TelegramLinkService : ITelegramLinkService
    {
        private readonly ITelegramLinkRepository _telegramLinkRepo;
        private readonly INhanVienRepository _nhanVienRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramLinkService> _logger;

        public TelegramLinkService(
            ITelegramLinkRepository telegramLinkRepo,
            INhanVienRepository nhanVienRepo,
            IConfiguration configuration,
            ILogger<TelegramLinkService> logger)
        {
            _telegramLinkRepo = telegramLinkRepo;
            _nhanVienRepo = nhanVienRepo;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TelegramLinkResponseDto> GenerateDeepLinkAsync(Guid nhanVienId, string? ipAddress)
        {
            // Tìm nhân viên
            var nhanVien = await _nhanVienRepo.GetByIdAsync(nhanVienId);
            if (nhanVien == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin nhân viên");
            }

            // Kiểm tra đã liên kết chưa
            if (!string.IsNullOrEmpty(nhanVien.TelegramChatId))
            {
                throw new InvalidOperationException($"Tài khoản đã được liên kết với Telegram (ChatId: {nhanVien.TelegramChatId})");
            }

            // Xóa các token cũ chưa dùng
            var oldTokens = await _telegramLinkRepo.GetUnusedTokensByNhanVienAsync(nhanVien.Id);
            if (oldTokens.Any())
            {
                await _telegramLinkRepo.DeleteRangeAsync(oldTokens);
            }

            // Tạo token mới
            var token = Guid.NewGuid().ToString("N");
            var expiryMinutes = _configuration.GetValue<int>("Telegram:LinkTokenExpiryMinutes", 10);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var linkToken = new TelegramLinkToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                NhanVienId = nhanVien.Id,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress
            };

            await _telegramLinkRepo.CreateAsync(linkToken);

            // Tạo deep link
            var botUsername = _configuration["Telegram:BotUsername"] ?? "company_manager_sp_bot";
            var deepLink = $"https://t.me/{botUsername}?start={token}";

            _logger.LogInformation($"Đã tạo deep link cho nhân viên {nhanVien.TenDayDu} (ID: {nhanVien.Id})");

            return new TelegramLinkResponseDto
            {
                DeepLink = deepLink,
                Token = token,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds
            };
        }

        public async Task<TelegramLinkStatusDto> GetLinkStatusAsync(Guid nhanVienId)
        {
            var nhanVien = await _nhanVienRepo.GetByIdAsync(nhanVienId);
            if (nhanVien == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin nhân viên");
            }

            var isLinked = !string.IsNullOrEmpty(nhanVien.TelegramChatId);

            // Tìm token đang chờ (nếu có)
            var pendingToken = await _telegramLinkRepo.GetPendingTokenAsync(nhanVien.Id);

            return new TelegramLinkStatusDto
            {
                IsLinked = isLinked,
                ChatId = nhanVien.TelegramChatId,
                HasPendingToken = pendingToken != null,
                PendingTokenExpiresAt = pendingToken?.ExpiresAt,
                PendingTokenExpiresInSeconds = pendingToken != null
                    ? (int)(pendingToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
                    : 0
            };
        }

        public async Task UnlinkTelegramAsync(Guid nhanVienId)
        {
            var nhanVien = await _nhanVienRepo.GetByIdAsync(nhanVienId);
            if (nhanVien == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin nhân viên");
            }

            if (string.IsNullOrEmpty(nhanVien.TelegramChatId))
            {
                throw new InvalidOperationException("Tài khoản chưa được liên kết với Telegram");
            }

            // Xóa ChatId
            nhanVien.TelegramChatId = null;
            await _nhanVienRepo.UpdateAsync(nhanVien);

            _logger.LogInformation($"Đã hủy liên kết Telegram cho nhân viên {nhanVien.TenDayDu} (ID: {nhanVien.Id})");
        }

        public async Task<TokenVerificationDto> VerifyTokenAsync(string token)
        {
            var linkToken = await _telegramLinkRepo.GetByTokenAsync(token, includeNhanVien: true);

            if (linkToken == null)
            {
                return new TokenVerificationDto
                {
                    Valid = false,
                    Message = "Token không tồn tại"
                };
            }

            if (linkToken.IsUsed)
            {
                return new TokenVerificationDto
                {
                    Valid = false,
                    Message = "Token đã được sử dụng",
                    UsedAt = linkToken.UsedAt
                };
            }

            if (linkToken.ExpiresAt < DateTime.UtcNow)
            {
                return new TokenVerificationDto
                {
                    Valid = false,
                    Message = "Token đã hết hạn",
                    ExpiresAt = linkToken.ExpiresAt
                };
            }

            return new TokenVerificationDto
            {
                Valid = true,
                Message = "Token hợp lệ",
                NhanVienName = linkToken.NhanVien?.TenDayDu,
                ExpiresAt = linkToken.ExpiresAt,
                ExpiresInSeconds = (int)(linkToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
            };
        }
    }
}
