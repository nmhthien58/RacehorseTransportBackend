using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.API.Middlewares
{
    // Bắt mọi exception và trả về đúng khuôn ApiResponse, nên controller không cần try/catch.
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                int statusCode = ex switch
                {
                    BusinessException => StatusCodes.Status400BadRequest,
                    UnauthorizedException => StatusCodes.Status401Unauthorized,
                    ForbiddenException => StatusCodes.Status403Forbidden,
                    NotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };

                string message = ex.Message;
                if (statusCode == StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

                    // Không lộ chi tiết lỗi nội bộ ra ngoài khi chạy thật.
                    if (!_environment.IsDevelopment())
                    {
                        message = "An internal server error occurred.";
                    }
                }

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(ApiResponse<object?>.ErrorResult(message));
            }
        }
    }
}
