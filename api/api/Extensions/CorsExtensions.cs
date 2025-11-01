namespace api.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình CORS
    /// </summary>
    public static class CorsExtensions
    {
        public const string AllowAngularClientPolicy = "AllowAngularClient";

        /// <summary>
        /// Cấu hình CORS cho Angular client
        /// </summary>
        public static IServiceCollection AddApplicationCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AllowAngularClientPolicy, policy =>
                {
                    policy.WithOrigins("http://localhost:4200") // Angular dev server
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });

                // Có thể thêm policy khác cho production
                // options.AddPolicy("Production", policy =>
                // {
                //     policy.WithOrigins("https://yourdomain.com")
                //           .AllowAnyHeader()
                //           .AllowAnyMethod();
                // });
            });

            return services;
        }
    }
}
