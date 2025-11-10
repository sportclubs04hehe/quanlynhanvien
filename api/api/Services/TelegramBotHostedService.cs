using api.Service.Interface;

namespace api.Services
{
    /// <summary>
    /// Background Service để chạy Telegram Bot Polling liên tục
    /// </summary>
    public class TelegramBotHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramBotHostedService> _logger;

        public TelegramBotHostedService(
            IServiceProvider serviceProvider,
            ILogger<TelegramBotHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Khởi động service khi app start
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Telegram Bot Hosted Service đang khởi động...");

            try
            {
                // Tạo scope để lấy ITelegramService
                using var scope = _serviceProvider.CreateScope();
                var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

                // Bắt đầu lắng nghe
                await telegramService.StartReceivingAsync(cancellationToken);

                _logger.LogInformation("✅ Telegram Bot Hosted Service đã khởi động thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khởi động Telegram Bot Hosted Service");
            }
        }

        /// <summary>
        /// Dừng service khi app shutdown
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Telegram Bot Hosted Service đang dừng...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

                await telegramService.StopReceivingAsync();

                _logger.LogInformation("✅ Telegram Bot Hosted Service đã dừng");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi dừng Telegram Bot Hosted Service");
            }
        }
    }
}
