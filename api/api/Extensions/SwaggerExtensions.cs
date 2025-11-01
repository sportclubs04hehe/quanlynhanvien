using Microsoft.OpenApi.Models;

namespace api.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình Swagger/OpenAPI
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Cấu hình Swagger với JWT Bearer Authentication
        /// </summary>
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                // API Info
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Company Manager API",
                    Version = "v1",
                    Description = "API quản lý nhân viên công ty",
                    Contact = new OpenApiContact
                    {
                        Name = "Dev Le Minh Huy",
                        Email = "vegakinvietnam@gmail.com"
                    }
                });

                // JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                                  "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                                  "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Có thể thêm XML comments sau
                // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                // options.IncludeXmlComments(xmlPath);
            });

            return services;
        }
    }
}
