using System.Net;
using System.Text.Json;
using InteractHub.Api.Enums;

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
            // Ghi log ra console của Render
            _logger.LogError(ex, "Đã xảy ra lỗi hệ thống: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = (int)HttpStatusCode.InternalServerError;
        var errorCode = ErrorCode.INTERNAL_SERVER_ERROR;

        // BUNG CHI TIẾT LỖI ĐỂ DEBUG (Bao gồm cả InnerException cực kỳ quan trọng đối với lỗi DB)
        var message = exception.InnerException != null
            ? $"Lỗi chính: {exception.Message} | Chi tiết sâu: {exception.InnerException.Message}"
            : exception.Message;

        // PHIÊN DỊCH LỖI: Chuyển các Exception C# thành HTTP Status Code
        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Forbidden;
                message = exception.Message;
                break;
            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case ArgumentException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            case InvalidOperationException:
                statusCode = (int)HttpStatusCode.Conflict;
                message = exception.Message;
                errorCode = ErrorCode.CONFLICT;
                break;
        }

        context.Response.StatusCode = statusCode;

        // Có thể in thêm StackTrace nếu bạn muốn xem lỗi nằm ở file nào, dòng số mấy
        var response = new
        {
            code = statusCode,
            message = message,
            errorCode = errorCode,
            // stackTrace = exception.StackTrace // Bỏ comment dòng này nếu muốn xem chi tiết đến từng dòng code
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}