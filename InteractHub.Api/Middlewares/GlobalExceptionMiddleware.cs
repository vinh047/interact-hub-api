using System.Net;
using System.Text.Json;

namespace InteractHub.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Cho phép Request đi tiếp vào bên trong (chạy qua Controller, Service...)
            await _next(context);
        }
        catch (Exception ex)
        {
            // Nếu có MỘT LỖI BẤT KỲ ném ra từ bên trong, tấm lưới này sẽ tóm lại!
            _logger.LogError(ex, "Đã xảy ra lỗi hệ thống: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Mặc định mọi lỗi không lường trước được tính là lỗi 500 (Sập server)
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = "Đã xảy ra lỗi hệ thống cục bộ.";

        // PHIÊN DỊCH LỖI: Chuyển các Exception C# thành HTTP Status Code
        switch (exception)
        {
            case UnauthorizedAccessException: 
                // Lỗi 403 do bạn ném ra ở BaseController hoặc Service
                statusCode = (int)HttpStatusCode.Forbidden;
                message = exception.Message;
                break;
            case KeyNotFoundException: 
                // Nếu sau này bạn có logic ném lỗi không tìm thấy (404)
                statusCode = (int)HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            // Bạn có thể thêm các case khác như ArgumentException -> 400 BadRequest
        }

        context.Response.StatusCode = statusCode;

        // Đóng gói lại thành JSON (Ở đây tôi dùng cấu trúc cơ bản, bạn có thể thay bằng ErrorResponse của bạn)
        var response = new 
        { 
            code = statusCode,
            message = message 
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}