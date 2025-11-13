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

            _logger.LogInformation("🔔 [TELEGRAM] Bắt đầu gửi thông báo đơn ID: {DonId}, Người gửi: {NguoiGui}",
                donYeuCau.Id, nguoiGui.TenDayDu);

            if (_botClient == null || !_isEnabled)
            {
                _logger.LogWarning("⚠️ [TELEGRAM] Bot không được bật hoặc chưa cấu hình. IsEnabled: {IsEnabled}, BotClient: {BotClient}",
                    _isEnabled, _botClient != null);
                return messageIds;
            }

            try
            {
                // Tìm giám đốc (hoặc trưởng phòng) để gửi thông báo
                _logger.LogInformation("🔍 [TELEGRAM] Đang tìm người duyệt...");
                var nguoiNhanThongBao = await TimNguoiDuyetDonAsync(donYeuCau, nguoiGui);

                if (nguoiNhanThongBao == null)
                {
                    _logger.LogWarning("⚠️ [TELEGRAM] Không tìm thấy người duyệt (Giám Đốc hoặc Trưởng Phòng) cho đơn ID: {DonId}", donYeuCau.Id);
                    return messageIds;
                }

                if (string.IsNullOrEmpty(nguoiNhanThongBao.TelegramChatId))
                {
                    _logger.LogWarning("⚠️ [TELEGRAM] Người duyệt {NguoiDuyet} chưa liên kết Telegram", nguoiNhanThongBao.TenDayDu);
                    return messageIds;
                }

                _logger.LogInformation("✅ [TELEGRAM] Tìm thấy người duyệt: {NguoiDuyet}, ChatId: {ChatId}",
                    nguoiNhanThongBao.TenDayDu, nguoiNhanThongBao.TelegramChatId);

                _logger.LogInformation("✅ [TELEGRAM] Tìm thấy người duyệt: {NguoiDuyet}, ChatId: {ChatId}",
                    nguoiNhanThongBao.TenDayDu, nguoiNhanThongBao.TelegramChatId);

                // Tạo nội dung tin nhắn
                var message = TaoNoiDungThongBao(donYeuCau, nguoiGui);

                // Gửi tin nhắn
                _logger.LogInformation("📤 [TELEGRAM] Đang gửi message tới ChatId: {ChatId}...", nguoiNhanThongBao.TelegramChatId);
                var sentMessage = await _botClient.SendMessage(
                    chatId: nguoiNhanThongBao.TelegramChatId,
                    text: message,
                    parseMode: ParseMode.Html
                );

                messageIds.Add(nguoiNhanThongBao.TelegramChatId, sentMessage.MessageId);
                _logger.LogInformation("✅ [TELEGRAM] Gửi thành công! MessageId: {MessageId}", sentMessage.MessageId);

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
            _logger.LogInformation("🔍 [TELEGRAM] Tìm Giám Đốc có role '{Role}' và đã liên kết Telegram...",
                AppRolesExtensions.GiamDoc);

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
                _logger.LogInformation("✅ [TELEGRAM] Tìm thấy Giám Đốc: {TenGiamDoc}, ChatId: {ChatId}",
                    giamDoc.TenDayDu, giamDoc.TelegramChatId);
                return giamDoc;
            }

            _logger.LogWarning("⚠️ [TELEGRAM] Không tìm thấy Giám Đốc có role '{Role}' và đã liên kết Telegram",
                AppRolesExtensions.GiamDoc);

            // Ưu tiên 2: Tìm Trưởng phòng của người gửi (người quản lý trực tiếp)
            if (nguoiGui.QuanLyId.HasValue)
            {
                _logger.LogInformation("🔍 [TELEGRAM] Tìm Trưởng Phòng (QuanLyId: {QuanLyId})...", nguoiGui.QuanLyId.Value);

                var truongPhong = await _context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.Id == nguoiGui.QuanLyId.Value
                                             && !string.IsNullOrEmpty(nv.TelegramChatId));

                if (truongPhong != null)
                {
                    _logger.LogInformation("✅ [TELEGRAM] Tìm thấy Trưởng Phòng: {TenTruongPhong}, ChatId: {ChatId}",
                        truongPhong.TenDayDu, truongPhong.TelegramChatId);
                    return truongPhong;
                }

                _logger.LogWarning("⚠️ [TELEGRAM] Trưởng Phòng (ID: {QuanLyId}) chưa liên kết Telegram", nguoiGui.QuanLyId.Value);
            }
            else
            {
                _logger.LogWarning("⚠️ [TELEGRAM] Nhân viên {NhanVien} không có QuanLyId (không có trưởng phòng)",
                    nguoiGui.TenDayDu);
            }

            _logger.LogError("❌ [TELEGRAM] Không tìm thấy người duyệt nào (Giám Đốc hoặc Trưởng Phòng) có Telegram");
            return null;
        }

        /// <summary>
        /// Tạo nội dung thông báo format HTML cho Telegram
        /// </summary>
        private string TaoNoiDungThongBao(DonYeuCau donYeuCau, NhanVien nguoiGui, bool daDuyet = false)
        {
            var loaiDon = donYeuCau.LoaiDon switch
            {
                LoaiDonYeuCau.NghiPhep => "ĐƠN XIN NGHỈ PHÉP",
                LoaiDonYeuCau.LamThemGio => "ĐƠN LÀM THÊM GIỜ",
                LoaiDonYeuCau.DiMuon => "ĐƠN ĐI MUỘN",
                LoaiDonYeuCau.CongTac => "ĐƠN CÔNG TÁC",
                _ => "📋 ĐƠN YÊU CẦU"
            };

            var message = $"<b>🔔 {loaiDon}</b>\n\n";
            message += $"<b>👤 Nhân viên:</b> {nguoiGui.TenDayDu}\n";
            message += $"<b>📅 Ngày tạo:</b> {donYeuCau.NgayTao:dd/MM/yyyy HH:mm}\n\n";

            // Thông tin chi tiết theo loại đơn
            switch (donYeuCau.LoaiDon)
            {
                case LoaiDonYeuCau.NghiPhep:
                    message += $"<b>📄 Loại đơn:</b> Nghỉ phép\n";
                    message += $"<b>📅 Thời gian nghỉ:</b> {donYeuCau.NgayBatDau:dd/MM/yyyy} → {donYeuCau.NgayKetThuc:dd/MM/yyyy}\n";
                    var soNgay = (donYeuCau.NgayKetThuc!.Value - donYeuCau.NgayBatDau!.Value).Days + 1;
                    message += $"<b>⏳ Tổng số ngày:</b> {soNgay} ngày\n";
                    break;

                case LoaiDonYeuCau.LamThemGio:
                    message += $"<b>📄 Loại đơn:</b> Làm thêm giờ\n";
                    message += $"<b>📅 Ngày làm thêm:</b> {donYeuCau.NgayLamThem:dd/MM/yyyy}\n";
                    message += $"<b>⏱️ Số giờ làm thêm:</b> {donYeuCau.SoGioLamThem} giờ\n";
                    break;

                case LoaiDonYeuCau.DiMuon:
                    message += $"<b>📄 Loại đơn:</b> Xin đi muộn\n";
                    message += $"<b>📅 Ngày:</b> {donYeuCau.NgayDiMuon:dd/MM/yyyy}\n";
                    message += $"<b>🕐 Giờ dự kiến đến:</b> {donYeuCau.GioDuKienDen:HH:mm}\n";
                    break;

                case LoaiDonYeuCau.CongTac:
                    message += $"<b>📄 Loại đơn:</b> Công tác\n";
                    message += $"<b>📅 Thời gian:</b> {donYeuCau.NgayBatDau:dd/MM/yyyy} → {donYeuCau.NgayKetThuc:dd/MM/yyyy}\n";
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
                              $"Tài khoản Telegram của bạn đã được liên kết với tài khoản: <b>{existingLink.TenDayDu}</b>\n\n" +
                              "Mỗi Telegram chỉ có thể liên kết với 1 tài khoản duy nhất.\n\n" +
                              "Nếu bạn muốn liên kết tài khoản mới:\n" +
                              "1️⃣ Đăng nhập tài khoản cũ và hủy liên kết\n" +
                              "2️⃣ Sau đó thử lại với tài khoản mới",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                    _logger.LogWarning($"⚠️ ChatId {chatId} đã liên kết với nhân viên {existingLink.TenDayDu}, không thể link với {nhanVien.TenDayDu}");
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
        /// Xử lý khi user nhập email
        /// ⚠️ DEPRECATED: Đã bị vô hiệu hóa vì lý do bảo mật
        /// Chỉ cho phép Deep Link authentication
        /// </summary>
        [Obsolete("Email authentication is disabled due to security concerns. Use Deep Link only.")]
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

            // ✅ KIỂM TRA 2 CHIỀU:
            // 1. Tài khoản này đã liên kết với Telegram khác chưa?
            if (!string.IsNullOrEmpty(nhanVien.TelegramChatId) && nhanVien.TelegramChatId != chatId.ToString())
            {
                await _botClient!.SendMessage(
                    chatId: chatId,
                    text: "⚠️ <b>Tài khoản này đã được liên kết với Telegram khác.</b>\n\n" +
                          "Nếu bạn muốn liên kết lại, vui lòng:\n" +
                          "1️⃣ Đăng nhập hệ thống web và hủy liên kết cũ\n" +
                          "2️⃣ Sau đó thử lại",
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
                          $"Tài khoản Telegram của bạn đã được liên kết với: <b>{existingLink.TenDayDu}</b>\n\n" +
                          "Mỗi Telegram chỉ có thể liên kết với 1 tài khoản duy nhất.\n\n" +
                          "Nếu bạn muốn liên kết tài khoản <b>{nhanVien.TenDayDu}</b>:\n" +
                          "1️⃣ Đăng nhập tài khoản cũ và hủy liên kết\n" +
                          "2️⃣ Sau đó thử lại",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                _logger.LogWarning($"⚠️ [EMAIL] ChatId {chatId} đã liên kết với {existingLink.TenDayDu}, không thể link với {nhanVien.TenDayDu}");
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
