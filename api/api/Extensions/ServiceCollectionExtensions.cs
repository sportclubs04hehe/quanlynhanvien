using api.Repository.Implement;
using api.Repository.Interface;
using api.Service.Implement;
using api.Service.Interface;

namespace api.Extensions
{
    /// <summary>
    /// Extension methods để đăng ký Repositories và Services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Đăng ký tất cả Repositories và Services vào DI Container
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IPhongBanRepository, PhongBanRepository>();
            services.AddScoped<IChucVuRepository, ChucVuRepository>();
            services.AddScoped<INhanVienRepository, NhanVienRepository>();
            services.AddScoped<IDonYeuCauRepository, DonYeuCauRepository>();
            services.AddScoped<ITelegramLinkRepository, TelegramLinkRepository>();
            services.AddScoped<INghiPhepQuotaRepository, NghiPhepQuotaRepository>();

            // Services
            services.AddScoped<IPhongBanService, PhongBanService>();
            services.AddScoped<IChucVuService, ChucVuService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDonYeuCauService, DonYeuCauService>();
            services.AddScoped<ITelegramLinkService, TelegramLinkService>();
            services.AddScoped<INghiPhepQuotaService, NghiPhepQuotaService>();

            return services;
        }
    }
}
