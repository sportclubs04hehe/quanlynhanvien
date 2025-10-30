using System.Net;
using System.Text.Json;

namespace api.Middleware
{
    public class CustomErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Capture the original response body stream
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                // Check if the response is 401 Unauthorized
                if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    context.Response.Body = originalBodyStream;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Email hoặc mật khẩu không đúng. Vui lòng kiểm tra lại.",
                        statusCode = 401
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
                else
                {
                    // Copy the response back to the original stream
                    context.Response.Body = originalBodyStream;
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }
    }
}
