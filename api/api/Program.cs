using api.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============ CONFIGURE SERVICES ============

// API Controllers with JSON options for proper DateTime serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize DateTime as ISO 8601 with UTC timezone (Z)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Không thêm ReferenceHandler để tránh circular references
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger/OpenAPI với JWT support
builder.Services.AddSwaggerWithJwt();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Application Services (Repositories & Services)
builder.Services.AddApplicationServices();

// Database (PostgreSQL + Entity Framework)
builder.Services.AddApplicationDatabase(builder.Configuration);

// Identity (User Management)
builder.Services.AddApplicationIdentity();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddApplicationCors();

// ============ BUILD APPLICATION ============

var app = builder.Build();

// ============ CONFIGURE MIDDLEWARE PIPELINE ============

// Seed database (Roles và Admin user)
await app.SeedDatabaseAsync();

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS (phải đặt trước Authentication/Authorization)
app.UseCors(CorsExtensions.AllowAngularClientPolicy);

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// ============ RUN APPLICATION ============

app.Run();
