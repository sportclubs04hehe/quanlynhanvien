using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace api.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình JWT Authentication
    /// </summary>
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Cấu hình JWT Bearer Authentication
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Đọc JWT settings từ appsettings.json
            var jwtKey = configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = configuration["Jwt:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer not configured");
            var jwtAudience = configuration["Jwt:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience not configured");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Set true in production
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero 
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}
