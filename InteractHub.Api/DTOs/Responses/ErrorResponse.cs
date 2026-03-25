using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Responses;

public class ErrorResponse
{
    public ErrorCode ErrorCode { get; set; } 

    public string Message { get; set; }

    public object? Details { get; set; } 

    public ErrorResponse(ErrorCode errorCode, string message, object? details = null)
    {
        ErrorCode = errorCode;
        Message = message;
        Details = details;
    }
}