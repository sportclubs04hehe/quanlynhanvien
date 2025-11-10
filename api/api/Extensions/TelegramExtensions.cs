using api.Service.Implement;
using api.Service.Interface;
using api.Services;

namespace api.Extensions
{
    /// <summary>
    /// Extension để đăng ký Telegram Service
    /// </summary>
    public static class TelegramExtensions
    {
        /// <summary>
        /// Đăng ký Telegram Bot Service vào DI container
        /// </summary>
        public static IServiceCollection AddTelegramBot(this IServiceCollection services)
        {
            // Đăng ký service như Scoped vì cần truy cập ApplicationDbContext (Scoped)
            services.AddScoped<ITelegramService, TelegramService>();

            // Đăng ký Hosted Service để chạy bot polling
            services.AddHostedService<TelegramBotHostedService>();

            return services;
        }

        /// <summary>
        /// Khởi tạo và test kết nối Telegram Bot (optional)
        /// </summary>
        public static async Task<IApplicationBuilder> UseTelegramBotAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TelegramService>>();

            try
            {
                var isConnected = await telegramService.KiemTraKetNoiAsync();
                if (isConnected)
                {
                    logger.LogInformation("✅ Telegram Bot đã sẵn sàng");
                }
                else
                {
                    logger.LogWarning("⚠️ Telegram Bot chưa được cấu hình hoặc không kết nối được");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Lỗi kiểm tra kết nối Telegram Bot");
            }

            return app;
        }
    }
}
