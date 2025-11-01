using api.Data;
using api.Model;
using Microsoft.AspNetCore.Identity;

namespace api.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình Identity
    /// </summary>
    public static class IdentityExtensions
    {
        /// <summary>
        /// Cấu hình ASP.NET Core Identity với custom password rules
        /// </summary>
        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
        {
            services.AddIdentityCore<User>(options =>
            {
                // Password settings - Nới lỏng để phát triển, nên tăng cường ở production
                options.Password.RequireDigit = false;           // Không yêu cầu số
                options.Password.RequireLowercase = false;       // Không yêu cầu chữ thường
                options.Password.RequireUppercase = false;       // Không yêu cầu chữ hoa
                options.Password.RequireNonAlphanumeric = false; // Không yêu cầu ký tự đặc biệt
                options.Password.RequiredLength = 6;             // Độ dài tối thiểu 6 ký tự
                options.Password.RequiredUniqueChars = 1;        // Số ký tự unique tối thiểu

                // User settings
                options.User.RequireUniqueEmail = true;

                // Lockout settings (có thể bật sau)
                // options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                // options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddSignInManager()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            return services;
        }
    }
}
