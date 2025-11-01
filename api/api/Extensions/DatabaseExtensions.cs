using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình Database
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Cấu hình PostgreSQL Database với Entity Framework Core
        /// </summary>
        public static IServiceCollection AddApplicationDatabase(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string not configured");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);

                // Có thể thêm các options khác
                // options.EnableSensitiveDataLogging(); // Chỉ dùng trong Development
                // options.EnableDetailedErrors();
            });

            return services;
        }

        /// <summary>
        /// Seed dữ liệu ban đầu (Roles, Admin user)
        /// </summary>
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                await DatabaseSeeder.SeedAsync(services);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw; // Re-throw để app không start nếu seed fail
            }
        }
    }
}
