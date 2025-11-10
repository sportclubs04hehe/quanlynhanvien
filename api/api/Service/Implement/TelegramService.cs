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
                _logger.LogWarning("⚠️ Telegram không được bật hoặc chưa cấu hình");
                return messageIds;
            }

            try
            {
                // Tìm giám đốc (hoặc trưởng phòng) để gửi thông báo
                var nguoiNhanThongBao = await TimNguoiDuyetDonAsync(donYeuCau, nguoiGui);

                if (nguoiNhanThongBao == null || string.IsNullOrEmpty(nguoiNhanThongBao.TelegramChatId))
                    return messageIds;

                // Tạo nội dung tin nhắn
                var message = TaoNoiDungThongBao(donYeuCau, nguoiGui);

                // Gửi tin nhắn
                var sentMessage = await _botClient.SendMessage(
                    chatId: nguoiNhanThongBao.TelegramChatId,
                    text: message,
                    parseMode: ParseMode.Html
                );

                messageIds.Add(nguoiNhanThongBao.TelegramChatId, sentMessage.MessageId);
                return messageIds;
            }
            catch (ApiRequestException ex)
            {
                _logger.LogError(ex, $"❌ Lỗi API Telegram: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi gửi thông báo Telegram");
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

            if (string.IsNullOrEmpty(donYeuCau.TelegramMessageIds))
                return;

            try
            {
                // Parse message IDs từ JSON
                var messageIds = JsonSerializer.Deserialize<Dictionary<string, long>>(donYeuCau.TelegramMessageIds);
                if (messageIds == null || !messageIds.Any())
                    return;

                // Tạo nội dung cập nhật
                var nguoiGui = await _context.NhanViens.FindAsync(donYeuCau.NhanVienId);
                var message = TaoNoiDungThongBao(donYeuCau, nguoiGui!, true);

                // Cập nhật từng message
                foreach (var (chatId, messageId) in messageIds)
                {
                    try
                    {
                        await _botClient.EditMessageText(
                            chatId: chatId,
                            messageId: (int)messageId,
                            text: message,
                            parseMode: ParseMode.Html
                        );
                    }
                    catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
                    {
                        // Message không thay đổi, bỏ qua
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi cập nhật message Telegram");
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
                _logger.LogInformation($"✅ Bot đang hoạt động: @{me.Username}");
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
            // Ưu tiên 1: Tìm Giám Đốc (role = GiamDoc)
            var giamDoc = await _context.NhanViens
                .Include(nv => nv.User)
                .Include(nv => nv.ChucVu)
                .Where(nv => nv.ChucVu != null && nv.ChucVu.TenChucVu.Contains("Giám Đốc"))
                .FirstOrDefaultAsync();

            if (giamDoc != null && !string.IsNullOrEmpty(giamDoc.TelegramChatId))
                return giamDoc;

            // Ưu tiên 2: Tìm Trưởng phòng của người gửi
            if (nguoiGui.QuanLyId.HasValue)
            {
                var truongPhong = await _context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.Id == nguoiGui.QuanLyId.Value);

                if (truongPhong != null && !string.IsNullOrEmpty(truongPhong.TelegramChatId))
                    return truongPhong;
            }

            return null;
        }

        /// <summary>
        /// Tạo nội dung thông báo format HTML cho Telegram
        /// </summary>
        private string TaoNoiDungThongBao(DonYeuCau donYeuCau, NhanVien nguoiGui, bool daDuyet = false)
        {
            var loaiDon = donYeuCau.LoaiDon switch
            {
                LoaiDonYeuCau.NghiPhep => "🏖️ NGHỈ PHÉP",
                LoaiDonYeuCau.LamThemGio => "⏰ LÀM THÊM GIỜ",
                LoaiDonYeuCau.DiMuon => "🕐 ĐI MUỘN",
                LoaiDonYeuCau.CongTac => "✈️ CÔNG TÁC",
                _ => "📋 ĐƠN YÊU CẦU"
            };

            var message = $"<b>🔔 {loaiDon}</b>\n\n";
            message += $"<b>👤 Nhân viên:</b> {nguoiGui.TenDayDu}\n";
            message += $"<b>📅 Ngày tạo:</b> {donYeuCau.NgayTao:dd/MM/yyyy HH:mm}\n\n";

            // Thông tin chi tiết theo loại đơn
            switch (donYeuCau.LoaiDon)
            {
                case LoaiDonYeuCau.NghiPhep:
                    message += $"<b>📅 Từ ngày:</b> {donYeuCau.NgayBatDau:dd/MM/yyyy}\n";
                    message += $"<b>📅 Đến ngày:</b> {donYeuCau.NgayKetThuc:dd/MM/yyyy}\n";
                    var soNgay = (donYeuCau.NgayKetThuc!.Value - donYeuCau.NgayBatDau!.Value).Days + 1;
                    message += $"<b>⏳ Số ngày:</b> {soNgay} ngày\n";
                    break;

                case LoaiDonYeuCau.LamThemGio:
                    message += $"<b>📅 Ngày:</b> {donYeuCau.NgayLamThem:dd/MM/yyyy}\n";
                    message += $"<b>⏱️ Số giờ:</b> {donYeuCau.SoGioLamThem} giờ\n";
                    break;

                case LoaiDonYeuCau.DiMuon:
                    message += $"<b>📅 Ngày:</b> {donYeuCau.NgayDiMuon:dd/MM/yyyy}\n";
                    message += $"<b>🕐 Giờ dự kiến đến:</b> {donYeuCau.GioDuKienDen:HH:mm}\n";
                    break;

                case LoaiDonYeuCau.CongTac:
                    message += $"<b>📅 Từ ngày:</b> {donYeuCau.NgayBatDau:dd/MM/yyyy}\n";
                    message += $"<b>📅 Đến ngày:</b> {donYeuCau.NgayKetThuc:dd/MM/yyyy}\n";
                    message += $"<b>📍 Địa điểm:</b> {donYeuCau.DiaDiemCongTac}\n";
                    message += $"<b>🎯 Mục đích:</b> {donYeuCau.MucDichCongTac}\n";
                    break;
            }

            message += $"\n<b>📝 Lý do:</b> {donYeuCau.LyDo}\n\n";

            // Trạng thái
            if (daDuyet)
            {
                var trangThai = donYeuCau.TrangThai switch
                {
                    TrangThaiDon.DaChapThuan => "✅ ĐÃ CHẤP THUẬN",
                    TrangThaiDon.BiTuChoi => "❌ BỊ TỪ CHỐI",
                    TrangThaiDon.DaHuy => "🚫 ĐÃ HỦY",
                    _ => "⏳ ĐANG CHỜ DUYỆT"
                };

                message += $"<b>🔖 Trạng thái:</b> {trangThai}\n";

                if (!string.IsNullOrEmpty(donYeuCau.GhiChuNguoiDuyet))
                {
                    message += $"<b>💬 Ghi chú:</b> {donYeuCau.GhiChuNguoiDuyet}\n";
                }

                if (donYeuCau.NgayDuyet.HasValue)
                {
                    message += $"<b>📅 Ngày duyệt:</b> {donYeuCau.NgayDuyet:dd/MM/yyyy HH:mm}\n";
                }
            }
            else
            {
                message += "<b>⏳ Trạng thái:</b> ĐANG CHỜ DUYỆT\n\n";
                message += "👉 Vui lòng vào hệ thống để duyệt đơn";
            }

            return message;
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

            _logger.LogInformation("🤖 Telegram Bot đang lắng nghe...");
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

            // Kiểm tra user đang ở bước nào (email flow - fallback)
            if (_userStates.TryGetValue(chatId, out var state))
            {
                if (state.CurrentStep == TelegramConversationSteps.AwaitingEmail)
                {
                    await HandleEmailInputAsync(chatId, messageText, cancellationToken);
                }
            }
            else
            {
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "👋 Chào bạn!\n\n" +
                          "Để liên kết tài khoản, vui lòng:\n" +
                          "1️⃣ Đăng nhập vào hệ thống web\n" +
                          "2️⃣ Vào phần Cài đặt → Telegram\n" +
                          "3️⃣ Click nút \"Liên kết Telegram\"\n" +
                          "4️⃣ Click vào link được tạo ra\n\n" +
                          "<i>Hoặc gửi /start nếu bạn muốn liên kết bằng email.</i>",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
            }
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

                // Kiểm tra tài khoản đã liên kết với Telegram khác chưa
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

                // ✅ Liên kết thành công
                nhanVien.TelegramChatId = chatId.ToString();
                linkToken.IsUsed = true;
                linkToken.UsedAt = DateTime.UtcNow;
                linkToken.TelegramChatId = chatId;

                await dbContext.SaveChangesAsync(cancellationToken);

                // Xóa state nếu có
                _userStates.TryRemove(chatId, out _);

                var successMessage = "✅ <b>Liên kết thành công!</b>\n\n" +
                                    $"👤 <b>Tài khoản:</b> {nhanVien.TenDayDu}\n" +
                                    $"📧 <b>Email:</b> {nhanVien.User?.Email}\n";

                if (nhanVien.ChucVu != null)
                {
                    successMessage += $"💼 <b>Chức vụ:</b> {nhanVien.ChucVu.TenChucVu}\n";
                }

                successMessage += "\n🔔 <b>Bạn sẽ nhận được thông báo qua Telegram khi:</b>\n" +
                                 "• Có đơn yêu cầu cần duyệt (nếu bạn là Giám đốc/Trưởng phòng)\n" +
                                 "• Đơn của bạn được duyệt/từ chối";

                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: successMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation($"✅ Deep link: Đã liên kết ChatId {chatId} với nhân viên {nhanVien.TenDayDu}");
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
                                "📧 <i>Hoặc bạn có thể liên kết bằng email (không khuyến nghị):</i>\n" +
                                "Nhập email đăng nhập của bạn vào đây.";

            await _botClient!.SendMessage(
                chatId: chatId,
                text: welcomeMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Tạo state cho email fallback (optional)
            _userStates[chatId] = new TelegramUserState
            {
                ChatId = chatId,
                CurrentStep = TelegramConversationSteps.AwaitingEmail
            };
        }

        /// <summary>
        /// Xử lý khi user nhập email
        /// </summary>
        private async Task HandleEmailInputAsync(long chatId, string email, CancellationToken cancellationToken)
        {
            email = email.Trim().ToLower();

            // Validate email format
            if (!IsValidEmail(email))
            {
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "❌ Email không hợp lệ. Vui lòng nhập lại email của bạn:",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Tạo scope mới để truy cập DB (vì polling chạy background)
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Tìm nhân viên trong DB
            var nhanVien = await dbContext.NhanViens
                .Include(n => n.User)
                .Include(n => n.ChucVu)
                .FirstOrDefaultAsync(n => n.User.Email!.ToLower() == email, cancellationToken);

            if (nhanVien == null)
            {
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "❌ <b>Không tìm thấy tài khoản với email này.</b>\n\n" +
                          "Vui lòng kiểm tra lại email hoặc liên hệ HR để được hỗ trợ.\n\n" +
                          "Nhập lại email hoặc gửi /start để bắt đầu lại.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Kiểm tra đã liên kết chưa
            if (!string.IsNullOrEmpty(nhanVien.TelegramChatId) && nhanVien.TelegramChatId != chatId.ToString())
            {
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "⚠️ <b>Tài khoản này đã được liên kết với Telegram khác.</b>\n\n" +
                          "Nếu bạn muốn liên kết lại, vui lòng liên hệ HR để được hỗ trợ.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                return;
            }

            // ✅ Liên kết thành công
            nhanVien.TelegramChatId = chatId.ToString();
            await dbContext.SaveChangesAsync(cancellationToken);

            // Xóa state
            _userStates.TryRemove(chatId, out _);

            var successMessage = "✅ <b>Liên kết thành công!</b>\n\n" +
                                $"👤 <b>Tài khoản:</b> {nhanVien.TenDayDu}\n" +
                                $"📧 <b>Email:</b> {nhanVien.User.Email}\n";

            if (nhanVien.ChucVu != null)
            {
                successMessage += $"💼 <b>Chức vụ:</b> {nhanVien.ChucVu.TenChucVu}\n";
            }

            successMessage += "\n🔔 Bạn sẽ nhận được thông báo qua Telegram khi có đơn yêu cầu cần duyệt.";

            await _botClient!.SendMessage(
                chatId: chatId,
                text: successMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// Xử lý callback queries (cho buttons)
        /// </summary>
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            // Implement sau nếu cần thêm buttons
            await _botClient!.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken
            );
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

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
