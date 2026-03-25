using System.Text.Json.Serialization;

namespace InteractHub.Api.Enums;

// Ép C# dịch Enum này thành chữ (String) khi trả về JSON
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorCode
{
    POST_NOT_FOUND,
    USER_NOT_FOUND,
    UNAUTHORIZED,
    FORBIDDEN_ACCESS,
    INTERNAL_SERVER_ERROR
}