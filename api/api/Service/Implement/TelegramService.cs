using api.Common;
using api.Data;
using api.Model;
using api.Model.Enums;
using api.Service.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace api.Service.Implement
{
    /// <summary>
    /// Service triển khai gửi thông báo qua Telegram Bot
    /// </summary>
    public class TelegramService : ITelegramService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TelegramService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TelegramBotClient? _botClient;
        private readonly bool _isEnabled;

        // State management cho conversations (in-memory)
        private static readonly ConcurrentDictionary<long, TelegramUserState> _userStates = new();
        private CancellationTokenSource? _receivingCancellationTokenSource;

        public TelegramService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<TelegramService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            // Đọc cấu hình từ appsettings
            var botToken = _configuration["Telegram:BotToken"];
            _isEnabled = _configuration.GetValue<bool>("Telegram:IsEnabled");

            // Khởi tạo bot client nếu có token và enabled
            if (!string.IsNullOrEmpty(botToken) && _isEnabled)
            {
                _botClient = new TelegramBotClient(botToken);
            }
        }

        /// <summary>
        /// Gửi thông báo đơn xin nghỉ đến giám đốc
        /// </summary>
        public async Task<Dictionary<string, long>> GuiThongBaoDonXinNghiAsync(DonYeuCau donYeuCau, NhanVien nguoiGui)
        {
            var messageIds = new Dictionary<string, long>();

            if (_botClient == null || !_isEnabled)
            {
                return messageIds;
            }

            try
            {
                // Tìm giám đốc (hoặc trưởng phòng) để gửi thông báo
                var nguoiNhanThongBao = await TimNguoiDuyetDonAsync(donYeuCau, nguoiGui);

                if (nguoiNhanThongBao == null)
                {
                    return messageIds;
                }

                if (string.IsNullOrEmpty(nguoiNhanThongBao.TelegramChatId))
                {
                    return messageIds;
                }

                // Tạo nội dung tin nhắn
                var message = TaoNoiDungThongBao(donYeuCau, nguoiGui);

                // Tạo Inline Keyboard với buttons Chấp thuận/Từ chối
                var inlineKeyboard = TaoInlineKeyboardChoDon(donYeuCau.Id);

                // Gửi tin nhắn với Inline Buttons
                var sentMessage = await _botClient.SendMessage(
                    chatId: nguoiNhanThongBao.TelegramChatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard
                );

                messageIds.Add(nguoiNhanThongBao.TelegramChatId, sentMessage.MessageId);

                return messageIds;
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi API Telegram: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi gửi thông báo Telegram");
                throw;
            }
        }

        /// <summary>
        /// Cập nhật message Telegram khi đơn được duyệt/từ chối
        /// </summary>
        public async Task CapNhatTrangThaiDonAsync(DonYeuCau donYeuCau, NhanVien nguoiDuyet)
        {
            if (_botClient == null || !_isEnabled)
                return;

            // Lấy thông tin nhân viên trước để kiểm tra
            var nguoiGui = await _context.NhanViens.FindAsync(donYeuCau.NhanVienId);
            if (nguoiGui == null)
                return;

            try
            {
                // Cập nhật message gốc (nếu có)
                if (!string.IsNullOrEmpty(donYeuCau.TelegramMessageIds))
                {
                    var messageIds = JsonSerializer.Deserialize<Dictionary<string, long>>(donYeuCau.TelegramMessageIds);
                    if (messageIds != null && messageIds.Any())
                    {
                        var message = TaoNoiDungThongBao(donYeuCau, nguoiGui!, true);

                        foreach (var (chatId, messageId) in messageIds)
                        {
                            try
                            {
                                await _botClient.EditMessageText(
                                    chatId: chatId,
                                    messageId: (int)messageId,
                                    text: message,
                                    parseMode: ParseMode.Html,
                                    replyMarkup: null
                                );
                            }
                            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
                            {
                                // Message không thay đổi, bỏ qua
                            }
                        }
                    }
                }

                // Gửi thông báo cho nhân viên (LUÔN GỬI khi duyệt qua Web API)
                if (!string.IsNullOrEmpty(nguoiGui.TelegramChatId))
                {
                    var notificationMessage = TelegramMessageBuilder.BuildEmployeeNotification(donYeuCau, nguoiDuyet);
                    
                    await _botClient.SendMessage(
                        chatId: nguoiGui.TelegramChatId,
                        text: notificationMessage,
                        parseMode: ParseMode.Html
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi cập nhật Telegram");
            }
        }

        /// <summary>
        /// Gửi tin nhắn tùy chỉnh
        /// </summary>
        public async Task<long?> GuiTinNhanAsync(string chatId, string message)
        {
            if (_botClient == null || !_isEnabled)
                return null;

            try
            {
                var sentMessage = await _botClient.SendMessage(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html
                );

                return sentMessage.MessageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi gửi tin nhắn Telegram");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra kết nối bot
        /// </summary>
        public async Task<bool> KiemTraKetNoiAsync()
        {
            if (_botClient == null || !_isEnabled)
                return false;

            try
            {
                var me = await _botClient.GetMe();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Không thể kết nối Telegram Bot");
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Tìm người duyệt đơn (Giám đốc hoặc Trưởng phòng)
        /// </summary>
        private async Task<NhanVien?> TimNguoiDuyetDonAsync(DonYeuCau donYeuCau, NhanVien nguoiGui)
        {
            // Ưu tiên 1: Tìm Giám Đốc (role = GiamDoc trong AspNetUserRoles)
            var giamDoc = await (from nv in _context.NhanViens
                                 join user in _context.Users on nv.Id equals user.Id
                                 join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                 join role in _context.Roles on userRole.RoleId equals role.Id
                                 where role.Name == AppRolesExtensions.GiamDoc
                                    && !string.IsNullOrEmpty(nv.TelegramChatId)
                                 select nv)
                                 .FirstOrDefaultAsync();

            if (giamDoc != null)
            {
                return giamDoc;
            }

            // Ưu tiên 2: Tìm Trưởng phòng của người gửi (người quản lý trực tiếp)
            if (nguoiGui.QuanLyId.HasValue)
            {
                var truongPhong = await _context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.Id == nguoiGui.QuanLyId.Value
                                             && !string.IsNullOrEmpty(nv.TelegramChatId));

                if (truongPhong != null)
                {
                    return truongPhong;
                }
            }

            _logger.LogError("❌ [TELEGRAM] Không tìm thấy người duyệt nào (Giám Đốc hoặc Trưởng Phòng) có Telegram");
            return null;
        }

        /// <summary>
        /// Tạo nội dung thông báo format HTML cho Telegram (wrapper for MessageBuilder)
        /// </summary>
        private string TaoNoiDungThongBao(DonYeuCau donYeuCau, NhanVien nguoiGui, bool daDuyet = false)
        {
            return daDuyet 
                ? TelegramMessageBuilder.BuildApprovedMessage(donYeuCau, nguoiGui)
                : TelegramMessageBuilder.BuildApprovalRequest(donYeuCau, nguoiGui);
        }

        /// <summary>
        /// Tạo Inline Keyboard cho đơn yêu cầu
        /// </summary>
        private InlineKeyboardMarkup TaoInlineKeyboardChoDon(Guid donId)
        {
            var keyboard = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Chấp thuận", $"approve_{donId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Từ chối", $"reject_{donId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📄 Chi tiết", $"details_{donId}")
                }
            };

            return new InlineKeyboardMarkup(keyboard);
        }

        #endregion

        #region Telegram Polling & Message Handling

        /// <summary>
        /// Bắt đầu lắng nghe messages từ Telegram
        /// </summary>
        public Task StartReceivingAsync(CancellationToken cancellationToken)
        {
            if (_botClient == null || !_isEnabled)
                return Task.CompletedTask;

            _receivingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                DropPendingUpdates = true
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _receivingCancellationTokenSource.Token
            );

            return Task.CompletedTask;
        }

        /// <summary>
        /// Dừng lắng nghe
        /// </summary>
        public Task StopReceivingAsync()
        {
            _receivingCancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Xử lý mỗi update từ Telegram
        /// </summary>
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is { } message)
                {
                    await HandleMessageAsync(message, cancellationToken);
                }
                else if (update.CallbackQuery is { } callbackQuery)
                {
                    await HandleCallbackQueryAsync(callbackQuery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý Telegram update");
            }
        }

        /// <summary>
        /// Xử lý text messages
        /// </summary>
        private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
        {
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            // Xử lý command /start với token (deep link)
            if (messageText.StartsWith("/start"))
            {
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 1)
                {
                    // /start TOKEN - Deep link authentication
                    var token = parts[1];
                    await HandleDeepLinkAuthenticationAsync(chatId, token, cancellationToken);
                }
                else
                {
                    // /start thông thường - Fallback sang email method (hoặc hiển thị hướng dẫn)
                    await HandleStartCommandAsync(chatId, cancellationToken);
                }
                return;
            }

            // Kiểm tra state: đang chờ nhập lý do từ chối?
            if (_userStates.TryGetValue(chatId, out var state) && state.State == "WAITING_REJECT_REASON" && state.DonIdToReject.HasValue)
            {
                await XuLyTuChoiDonAsync(chatId, state.DonIdToReject.Value, messageText, cancellationToken);
                return;
            }

            // ❌ Email flow đã bị XÓA vì lý do bảo mật
            // Chỉ hỗ trợ Deep Link authentication
            await _botClient!.SendMessage(
                chatId: chatId,
                text: "👋 <b>Chào bạn!</b>\n\n" +
                      "🔗 <b>Để liên kết tài khoản, vui lòng:</b>\n\n" +
                      "1️⃣ Đăng nhập vào hệ thống web\n" +
                      "2️⃣ Vào phần <b>Cài đặt</b> → <b>Telegram</b>\n" +
                      "3️⃣ Click nút <b>\"Liên kết Telegram\"</b>\n" +
                      "4️⃣ Click vào link được tạo ra\n\n" +
                      "🔒 Chỉ sử dụng link từ hệ thống web để đảm bảo bảo mật.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Xử lý Deep Link authentication với token
        /// </summary>
        private async Task HandleDeepLinkAuthenticationAsync(long chatId, string token, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Tìm token trong database
                var linkToken = await dbContext.TelegramLinkTokens
                    .Include(t => t.NhanVien)
                        .ThenInclude(n => n!.User)
                    .Include(t => t.NhanVien)
                        .ThenInclude(n => n!.ChucVu)
                    .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

                if (linkToken == null)
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: "❌ <b>Link không hợp lệ</b>\n\n" +
                              "Link liên kết không tồn tại hoặc đã hết hạn.\n\n" +
                              "Vui lòng tạo link mới từ hệ thống web.",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Kiểm tra token đã được sử dụng chưa
                if (linkToken.IsUsed)
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: "⚠️ <b>Link đã được sử dụng</b>\n\n" +
                              $"Link này đã được sử dụng lúc: {linkToken.UsedAt:dd/MM/yyyy HH:mm}\n\n" +
                              "Vui lòng tạo link mới từ hệ thống web.",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Kiểm tra token đã hết hạn chưa
                if (linkToken.ExpiresAt < DateTime.UtcNow)
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: "⏰ <b>Link đã hết hạn</b>\n\n" +
                              $"Link này đã hết hạn lúc: {linkToken.ExpiresAt:dd/MM/yyyy HH:mm}\n\n" +
                              "Vui lòng tạo link mới từ hệ thống web.",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var nhanVien = linkToken.NhanVien;
                if (nhanVien == null)
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: "❌ Lỗi hệ thống. Vui lòng thử lại sau.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // ✅ KIỂM TRA 2 CHIỀU:
                // 1. Tài khoản này đã liên kết với Telegram khác chưa?
                if (!string.IsNullOrEmpty(nhanVien.TelegramChatId) &&
                    nhanVien.TelegramChatId != chatId.ToString())
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: "⚠️ <b>Tài khoản đã được liên kết</b>\n\n" +
                              "Tài khoản này đã được liên kết với Telegram khác.\n\n" +
                              "Nếu bạn muốn liên kết lại, vui lòng:\n" +
                              "1️⃣ Hủy liên kết cũ trên hệ thống web\n" +
                              "2️⃣ Tạo link mới và thử lại",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // 2. ChatId này đã liên kết với tài khoản khác chưa?
                var existingLink = await dbContext.NhanViens
                    .FirstOrDefaultAsync(n => n.TelegramChatId == chatId.ToString() && n.Id != nhanVien.Id, cancellationToken);

                if (existingLink != null)
                {
                    await _botClient!.SendMessage(
                        chatId: chatId,
                        text: $"⚠️ <b>Telegram này đã được liên kết</b>\n\n" +
                              $"Tài khoản Telegram của bạn đã được liên kết với 1 tài khoản khác\n\n" +
                              "Mỗi Telegram chỉ có thể liên kết với 1 tài khoản duy nhất.\n\n" +
                              "Nếu bạn muốn liên kết tài khoản mới:\n" +
                              "1️⃣ Đăng nhập tài khoản cũ và hủy liên kết\n" +
                              "2️⃣ Sau đó thử lại với tài khoản mới",
                        parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                    return;
                }                // ✅ Liên kết thành công
                nhanVien.TelegramChatId = chatId.ToString();
                linkToken.IsUsed = true;
                linkToken.UsedAt = DateTime.UtcNow;
                linkToken.TelegramChatId = chatId;

                await dbContext.SaveChangesAsync(cancellationToken);

                // Xóa state nếu có
                _userStates.TryRemove(chatId, out _);

                // Lấy role của nhân viên
                var userRoles = await dbContext.UserRoles
                    .Where(ur => ur.UserId == nhanVien.Id)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync(cancellationToken);

                var isGiamDoc = userRoles.Contains(AppRolesExtensions.GiamDoc);
                var isTruongPhong = userRoles.Contains(AppRolesExtensions.TruongPhong);

                // Tạo message chào mừng ngắn gọn
                var successMessage = $"✅ <b>Xin chào {nhanVien.TenDayDu}!</b>\n\n" +
                                    "🎉 Liên kết Telegram thành công!\n\n";

                // Hiển thị thông tin cơ bản
                if (nhanVien.ChucVu != null)
                {
                    successMessage += $"💼 {nhanVien.ChucVu.TenChucVu}";
                    if (nhanVien.PhongBan != null)
                    {
                        successMessage += $" - {nhanVien.PhongBan.TenPhongBan}";
                    }
                    successMessage += "\n\n";
                }
                else if (nhanVien.PhongBan != null)
                {
                    successMessage += $"🏢 {nhanVien.PhongBan.TenPhongBan}\n\n";
                }

                // Thông báo chức năng theo role
                if (isGiamDoc)
                {
                    successMessage += "📱 <b>Bạn sẽ nhận được:</b>\n" +
                                     "• Thông báo đơn yêu cầu từ nhân viên\n" +
                                     "• Duyệt/từ chối đơn trực tiếp trên Telegram\n\n" +
                                     "💡 Giữ Telegram mở để quản lý đơn kịp thời!";
                }
                else if (isTruongPhong)
                {
                    successMessage += "📱 <b>Bạn sẽ nhận được:</b>\n" +
                                     "• Thông báo đơn từ nhân viên trong phòng\n" +
                                     "• Duyệt/từ chối đơn trực tiếp trên Telegram\n" +
                                     "• Thông báo kết quả đơn của bạn\n\n" +
                                     "💡 Giữ Telegram mở để quản lý đơn kịp thời!";
                }
                else
                {
                    successMessage += "📱 <b>Bạn sẽ nhận được:</b>\n" +
                                     "• Thông báo khi đơn được duyệt/từ chối\n" +
                                     "• Chi tiết người duyệt và thời gian\n\n" +
                                     "💡 Giữ Telegram mở để nhận thông báo kịp thời!";
                }

                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: successMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi xử lý deep link authentication: {ex.Message}");
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "❌ Có lỗi xảy ra. Vui lòng thử lại sau.",
                    cancellationToken: cancellationToken
                );
            }
        }

        /// <summary>
        /// Xử lý /start command (không có token)
        /// </summary>
        private async Task HandleStartCommandAsync(long chatId, CancellationToken cancellationToken)
        {
            var welcomeMessage = "👋 <b>Chào mừng đến với Company Manager Bot!</b>\n\n" +
                                "🔗 <b>Để liên kết tài khoản Telegram:</b>\n\n" +
                                "1️⃣ Đăng nhập vào hệ thống web\n" +
                                "2️⃣ Vào <b>Cài đặt</b> → <b>Telegram</b>\n" +
                                "3️⃣ Click nút <b>\"Liên kết Telegram\"</b>\n" +
                                "4️⃣ Click vào link được tạo ra\n\n" +
                                "━━━━━━━━━━━━━━━━━━━\n\n" +
                                "🔒 <b>Lưu ý bảo mật:</b>\n" +
                                "• Link chỉ có hiệu lực 10 phút\n" +
                                "• Mỗi link chỉ sử dụng được 1 lần\n" +
                                "• Không chia sẻ link với người khác";

            await _botClient!.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // ❌ Không tạo state cho email flow nữa - chỉ dùng Deep Link
        }

        /// <summary>
        /// Xử lý callback queries (cho buttons)
        /// </summary>
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (_botClient == null || callbackQuery.Data == null || callbackQuery.Message == null)
                return;

            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            try
            {
                // Answer callback query ngay để Telegram không hiển thị loading
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                // Parse callback data: format "action_donId"
                var parts = data.Split('_');
                if (parts.Length != 2)
                {
                    await _botClient.SendMessage(chatId, "❌ Dữ liệu không hợp lệ", cancellationToken: cancellationToken);
                    return;
                }

                var action = parts[0];
                if (!Guid.TryParse(parts[1], out var donId))
                {
                    await _botClient.SendMessage(chatId, "❌ Mã đơn không hợp lệ", cancellationToken: cancellationToken);
                    return;
                }

                // Xử lý theo action
                switch (action)
                {
                    case "approve":
                        await XuLyChapThuanDonAsync(chatId, donId, callbackQuery.Message.MessageId, cancellationToken);
                        break;

                    case "reject":
                        await XuLyYeuCauNhapLyDoTuChoiAsync(chatId, donId, cancellationToken);
                        break;

                    case "details":
                        await XuLyXemChiTietDonAsync(chatId, donId, cancellationToken);
                        break;

                    default:
                        await _botClient.SendMessage(chatId, "❌ Hành động không được hỗ trợ", cancellationToken: cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi xử lý callback query");
                await _botClient.SendMessage(chatId, "❌ Đã xảy ra lỗi khi xử lý yêu cầu", cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Xử lý chấp thuận đơn qua Telegram
        /// </summary>
        private async Task XuLyChapThuanDonAsync(long chatId, Guid donId, int messageId, CancellationToken cancellationToken)
        {
            // Tạo scope mới để tránh ObjectDisposedException
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Tìm nhân viên duyệt dựa trên Telegram ChatId
                var nguoiDuyet = await context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.TelegramChatId == chatId.ToString());

                if (nguoiDuyet == null)
                {
                    await _botClient!.SendMessage(chatId, 
                        "❌ Không tìm thấy tài khoản liên kết với Telegram này", 
                        cancellationToken: cancellationToken);
                    return;
                }

                // Lấy thông tin đơn
                var don = await context.DonYeuCaus
                    .Include(d => d.NhanVien)
                    .FirstOrDefaultAsync(d => d.Id == donId);

                if (don == null)
                {
                    await _botClient!.SendMessage(chatId, 
                        "❌ Không tìm thấy đơn này", 
                        cancellationToken: cancellationToken);
                    return;
                }

                // Kiểm tra trạng thái đơn
                if (don.TrangThai != TrangThaiDon.DangChoDuyet)
                {
                    await _botClient!.SendMessage(chatId, 
                        $"⚠️ Đơn này đã được xử lý ({don.TrangThai})", 
                        cancellationToken: cancellationToken);
                    return;
                }

                // Cập nhật trạng thái đơn
                don.TrangThai = TrangThaiDon.DaChapThuan;
                don.DuocChapThuanBoi = nguoiDuyet.Id;
                don.NgayDuyet = DateTime.UtcNow;
                don.GhiChuNguoiDuyet = "Đã duyệt qua Telegram";

                await context.SaveChangesAsync(cancellationToken);

                // Edit message gốc - disable buttons
                var updatedMessage = TaoNoiDungThongBao(don, don.NhanVien, daDuyet: true);
                await _botClient!.EditMessageText(
                    chatId: chatId,
                    messageId: messageId,
                    text: updatedMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );

                // Gửi thông báo cho nhân viên (nếu có Telegram)
                if (!string.IsNullOrEmpty(don.NhanVien.TelegramChatId))
                {
                    var notificationMessage = TelegramMessageBuilder.BuildEmployeeNotification(don, nguoiDuyet);

                    await _botClient!.SendMessage(
                        chatId: don.NhanVien.TelegramChatId,
                        text: notificationMessage,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi chấp thuận đơn {DonId}", donId);
                await _botClient!.SendMessage(chatId, "❌ Đã xảy ra lỗi khi chấp thuận đơn", cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Yêu cầu nhập lý do từ chối
        /// </summary>
        private async Task XuLyYeuCauNhapLyDoTuChoiAsync(long chatId, Guid donId, CancellationToken cancellationToken)
        {
            // Set state: đang chờ nhập lý do từ chối
            _userStates[chatId] = new TelegramUserState
            {
                State = "WAITING_REJECT_REASON",
                DonIdToReject = donId
            };

            await _botClient!.SendMessage(
                chatId: chatId,
                text: "📝 Vui lòng nhập lý do từ chối đơn này:",
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Xử lý khi user nhập lý do từ chối
        /// </summary>
        private async Task XuLyTuChoiDonAsync(long chatId, Guid donId, string lyDoTuChoi, CancellationToken cancellationToken)
        {
            // Tạo scope mới để tránh ObjectDisposedException
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Tìm nhân viên duyệt
                var nguoiDuyet = await context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.TelegramChatId == chatId.ToString());

                if (nguoiDuyet == null)
                {
                    await _botClient!.SendMessage(chatId, "❌ Không tìm thấy tài khoản", cancellationToken: cancellationToken);
                    return;
                }

                // Lấy thông tin đơn
                var don = await context.DonYeuCaus
                    .Include(d => d.NhanVien)
                    .FirstOrDefaultAsync(d => d.Id == donId);

                if (don == null)
                {
                    await _botClient!.SendMessage(chatId, "❌ Không tìm thấy đơn này", cancellationToken: cancellationToken);
                    return;
                }

                // Kiểm tra trạng thái
                if (don.TrangThai != TrangThaiDon.DangChoDuyet)
                {
                    await _botClient!.SendMessage(chatId, $"⚠️ Đơn này đã được xử lý ({don.TrangThai})", cancellationToken: cancellationToken);
                    return;
                }

                // Cập nhật trạng thái
                don.TrangThai = TrangThaiDon.BiTuChoi;
                don.DuocChapThuanBoi = nguoiDuyet.Id;
                don.NgayDuyet = DateTime.UtcNow;
                don.GhiChuNguoiDuyet = lyDoTuChoi;

                await context.SaveChangesAsync(cancellationToken);

                // Thông báo thành công
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: $"✅ Đã từ chối đơn thành công\n\n📝 Lý do: {lyDoTuChoi}",
                    cancellationToken: cancellationToken
                );

                // Gửi thông báo cho nhân viên
                if (!string.IsNullOrEmpty(don.NhanVien.TelegramChatId))
                {
                    var notificationMessage = TelegramMessageBuilder.BuildEmployeeNotification(don, nguoiDuyet);

                    await _botClient!.SendMessage(
                        chatId: don.NhanVien.TelegramChatId,
                        text: notificationMessage,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                }

                // Clear state
                _userStates.TryRemove(chatId, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi từ chối đơn {DonId}", donId);
                await _botClient!.SendMessage(chatId, "❌ Đã xảy ra lỗi khi từ chối đơn", cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Xem chi tiết đơn
        /// </summary>
        private async Task XuLyXemChiTietDonAsync(long chatId, Guid donId, CancellationToken cancellationToken)
        {
            // Tạo scope mới để tránh ObjectDisposedException
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var don = await context.DonYeuCaus
                    .Include(d => d.NhanVien)
                        .ThenInclude(nv => nv.User)
                    .Include(d => d.NhanVien)
                        .ThenInclude(nv => nv.PhongBan)
                    .Include(d => d.NhanVien)
                        .ThenInclude(nv => nv.ChucVu)
                    .FirstOrDefaultAsync(d => d.Id == donId);

                if (don == null)
                {
                    await _botClient!.SendMessage(chatId, "❌ Không tìm thấy đơn này", cancellationToken: cancellationToken);
                    return;
                }

                var detailMessage = TelegramMessageBuilder.BuildDetailMessage(don);

                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: detailMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TELEGRAM] Lỗi xem chi tiết đơn {DonId}", donId);
                await _botClient!.SendMessage(chatId, "❌ Đã xảy ra lỗi khi xem chi tiết", cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Xử lý lỗi polling
        /// </summary>
        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiEx => $"Telegram API Error [{apiEx.ErrorCode}]: {apiEx.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, $"❌ Telegram Polling Error: {errorMessage}");
            return Task.CompletedTask;
        }
        #endregion

        #region Message Builder

        /// <summary>
        /// Utility class để tạo Telegram messages với format nhất quán
        /// Giảm duplicate code và dễ maintain
        /// </summary>
        private static class TelegramMessageBuilder
        {
            /// <summary>
            /// Tạo message cho yêu cầu duyệt đơn (gửi cho giám đốc/trưởng phòng)
            /// </summary>
            public static string BuildApprovalRequest(DonYeuCau don, NhanVien nguoiGui)
            {
                var header = GetLoaiDonHeader(don.LoaiDon);
                var message = $"<b>🔔 {header}</b>\n\n";
                message += $"<b>👤 Nhân viên:</b> {nguoiGui.TenDayDu}\n";
                var ngayTaoVN = ToVietnamTime(don.NgayTao);
                message += $"<b>📅 Ngày tạo:</b> {ngayTaoVN:dd/MM/yyyy HH:mm}\n\n";
                
                message += BuildDonDetails(don);
                message += $"\n<b>📝 Lý do:</b> {don.LyDo}\n\n";
                message += "<b>⏳ Trạng thái:</b> ĐANG CHỜ DUYỆT\n\n";
                message += "👉 Bạn có thể chấp thuận đơn tại đây hoặc truy cập vào hệ thống để duyệt đơn.";
                
                return message;
            }

            /// <summary>
            /// Tạo message khi đơn đã được duyệt/từ chối (update message gốc)
            /// </summary>
            public static string BuildApprovedMessage(DonYeuCau don, NhanVien nguoiGui)
            {
                var header = GetLoaiDonHeader(don.LoaiDon);
                var message = $"<b>🔔 {header}</b>\n\n";
                message += $"<b>👤 Nhân viên:</b> {nguoiGui.TenDayDu}\n";
                var ngayTaoVN = ToVietnamTime(don.NgayTao);
                message += $"<b>📅 Ngày tạo:</b> {ngayTaoVN:dd/MM/yyyy HH:mm}\n\n";
                
                message += BuildDonDetails(don);
                message += $"\n<b>📝 Lý do:</b> {don.LyDo}\n\n";
                message += BuildApprovalStatus(don);
                
                return message;
            }

            /// <summary>
            /// Tạo message thông báo cho nhân viên khi đơn được duyệt/từ chối
            /// </summary>
            public static string BuildEmployeeNotification(DonYeuCau don, NhanVien nguoiDuyet)
            {
                var (icon, status) = don.TrangThai == TrangThaiDon.DaChapThuan 
                    ? ("✅", "đã được chấp thuận!") 
                    : ("❌", "đã bị từ chối");

                var message = $"{icon} <b>Đơn của bạn {status}</b>\n\n";
                
                // Hiển thị mã đơn
                message += $"<b>🆔 Mã đơn:</b> {don.MaDon ?? don.Id.ToString()[..8]}\n";
                
                // Hiển thị chi tiết đơn (bao gồm loại đơn và thông tin chi tiết)
                message += BuildDonDetails(don);
                
                // Hiển thị lý do
                message += $"\n<b>📝 Lý do:</b> {don.LyDo}\n";
                
                // Thông tin duyệt
                message += $"\n<b>👤 Người duyệt:</b> {nguoiDuyet.TenDayDu}\n";
                var ngayDuyetVN = ToVietnamTime(don.NgayDuyet!.Value);
                message += $"<b>📅 Ngày duyệt:</b> {ngayDuyetVN:dd/MM/yyyy HH:mm}\n";
                
                // Hiển thị ghi chú/lý do từ chối
                if (!string.IsNullOrEmpty(don.GhiChuNguoiDuyet))
                {
                    if (don.TrangThai == TrangThaiDon.BiTuChoi)
                        message += $"\n<b>❌ Lý do từ chối:</b>\n{don.GhiChuNguoiDuyet}";
                    else
                        message += $"\n<b>💬 Ghi chú:</b>\n{don.GhiChuNguoiDuyet}";
                }
                
                return message;
            }

            /// <summary>
            /// Tạo message chi tiết đơn (khi click button "Chi tiết")
            /// </summary>
            public static string BuildDetailMessage(DonYeuCau don)
            {
                var message = "<b>📋 CHI TIẾT ĐƠN YÊU CẦU</b>\n\n";
                message += $"<b>🆔 Mã đơn:</b> {don.MaDon ?? don.Id.ToString()[..8]}\n";
                message += $"<b>📄 Loại:</b> {don.LoaiDon.ToDisplayName()}\n";
                message += $"<b>🔖 Trạng thái:</b> {don.TrangThai.ToDisplayName()}\n\n";
                message += $"<b>👤 Nhân viên:</b> {don.NhanVien.TenDayDu}\n";
                message += $"<b>📧 Email:</b> {don.NhanVien.User.Email}\n";
                message += $"<b>🏢 Phòng ban:</b> {don.NhanVien.PhongBan?.TenPhongBan ?? "Chưa có"}\n";
                message += $"<b>💼 Chức vụ:</b> {don.NhanVien.ChucVu?.TenChucVu ?? "Chưa có"}\n\n";
                message += $"<b>📝 Lý do:</b> {don.LyDo}\n";
                var ngayTaoVN = ToVietnamTime(don.NgayTao);
                message += $"<b>📅 Ngày tạo:</b> {ngayTaoVN:dd/MM/yyyy HH:mm}";
                
                return message;
            }

            #region Private Helpers

            /// <summary>
            /// Chuyển đổi DateTime từ UTC sang múi giờ Việt Nam (UTC+7)
            /// </summary>
            private static DateTime ToVietnamTime(DateTime utcDateTime)
            {
                return DateTimeHelper.ToVietnamTime(utcDateTime);
            }

            private static string GetLoaiDonHeader(LoaiDonYeuCau loaiDon) => loaiDon switch
            {
                LoaiDonYeuCau.NghiPhep => "ĐƠN XIN NGHỈ PHÉP",
                LoaiDonYeuCau.LamThemGio => "ĐƠN LÀM THÊM GIỜ",
                LoaiDonYeuCau.DiMuon => "ĐƠN ĐI MUỘN",
                LoaiDonYeuCau.CongTac => "ĐƠN CÔNG TÁC",
                _ => "📋 ĐƠN YÊU CẦU"
            };

            private static string BuildDonDetails(DonYeuCau don)
            {
                return don.LoaiDon switch
                {
                    LoaiDonYeuCau.NghiPhep => BuildNghiPhepDetails(don),
                    LoaiDonYeuCau.LamThemGio => BuildLamThemGioDetails(don),
                    LoaiDonYeuCau.DiMuon => BuildDiMuonDetails(don),
                    LoaiDonYeuCau.CongTac => BuildCongTacDetails(don),
                    _ => ""
                };
            }

            private static string BuildNghiPhepDetails(DonYeuCau don)
            {
                var details = "<b>📄 Loại đơn:</b> Nghỉ phép";
                
                // Hiển thị loại nghỉ phép chi tiết (sáng/chiều/cả ngày/nhiều ngày)
                if (don.LoaiNghiPhep.HasValue)
                {
                    var icon = don.LoaiNghiPhep.Value switch
                    {
                        LoaiNghiPhep.BuoiSang => "🌅",
                        LoaiNghiPhep.BuoiChieu => "🌆",
                        LoaiNghiPhep.MotNgay => "📅",
                        LoaiNghiPhep.NhieuNgay => "📆",
                        _ => "📋"
                    };
                    details += $" - {icon} <b>{don.LoaiNghiPhep.Value.ToDisplayName()}</b>";
                }
                details += "\n";
                
                // Hiển thị thời gian nghỉ
                if (don.LoaiNghiPhep == LoaiNghiPhep.BuoiSang || don.LoaiNghiPhep == LoaiNghiPhep.BuoiChieu)
                {
                    // Nửa ngày - chỉ hiển thị 1 ngày
                    details += $"<b>📅 Ngày nghỉ:</b> {don.NgayBatDau:dd/MM/yyyy}\n";
                    details += $"<b>⏳ Thời gian:</b> 0.5 ngày ({(don.LoaiNghiPhep == LoaiNghiPhep.BuoiSang ? "Buổi sáng" : "Buổi chiều")})\n";
                }
                else if (don.LoaiNghiPhep == LoaiNghiPhep.MotNgay)
                {
                    // 1 ngày
                    details += $"<b>📅 Ngày nghỉ:</b> {don.NgayBatDau:dd/MM/yyyy}\n";
                    details += $"<b>⏳ Thời gian:</b> 1 ngày (Cả ngày)\n";
                }
                else
                {
                    // Nhiều ngày hoặc không có loại nghỉ phép
                    var soNgay = (don.NgayKetThuc!.Value - don.NgayBatDau!.Value).Days + 1;
                    details += $"<b>📅 Thời gian nghỉ:</b> {don.NgayBatDau:dd/MM/yyyy} → {don.NgayKetThuc:dd/MM/yyyy}\n";
                    details += $"<b>⏳ Tổng số ngày:</b> {soNgay} ngày\n";
                }
                
                return details;
            }

            private static string BuildLamThemGioDetails(DonYeuCau don)
            {
                return $"<b>📄 Loại đơn:</b> Làm thêm giờ\n" +
                       $"<b>📅 Ngày làm thêm:</b> {don.NgayLamThem:dd/MM/yyyy}\n" +
                       $"<b>⏱️ Số giờ làm thêm:</b> {don.SoGioLamThem} giờ\n";
            }

            private static string BuildDiMuonDetails(DonYeuCau don)
            {
                return $"<b>📄 Loại đơn:</b> Xin đi muộn\n" +
                       $"<b>📅 Ngày:</b> {don.NgayDiMuon:dd/MM/yyyy}\n" +
                       $"<b>🕐 Giờ dự kiến đến:</b> {don.GioDuKienDen:HH:mm}\n";
            }

            private static string BuildCongTacDetails(DonYeuCau don)
            {
                return $"<b>📄 Loại đơn:</b> Công tác\n" +
                       $"<b>📅 Thời gian:</b> {don.NgayBatDau:dd/MM/yyyy} → {don.NgayKetThuc:dd/MM/yyyy}\n" +
                       $"<b>📍 Địa điểm:</b> {don.DiaDiemCongTac}\n" +
                       $"<b>🎯 Mục đích:</b> {don.MucDichCongTac}\n";
            }

            private static string BuildApprovalStatus(DonYeuCau don)
            {
                var trangThai = don.TrangThai switch
                {
                    TrangThaiDon.DaChapThuan => "✅ ĐÃ CHẤP THUẬN",
                    TrangThaiDon.BiTuChoi => "❌ BỊ TỪ CHỐI",
                    TrangThaiDon.DaHuy => "🚫 ĐÃ HỦY",
                    _ => "⏳ ĐANG CHỜ DUYỆT"
                };

                var message = $"<b>🔖 Trạng thái:</b> {trangThai}\n";

                if (!string.IsNullOrEmpty(don.GhiChuNguoiDuyet))
                    message += $"<b>💬 Ghi chú:</b> {don.GhiChuNguoiDuyet}\n";

                if (don.NgayDuyet.HasValue)
                {
                    var ngayDuyetVN = ToVietnamTime(don.NgayDuyet.Value);
                    message += $"<b>📅 Ngày duyệt:</b> {ngayDuyetVN:dd/MM/yyyy HH:mm}\n";
                }

                return message;
            }

            #endregion
        }

        #endregion
    }
}
