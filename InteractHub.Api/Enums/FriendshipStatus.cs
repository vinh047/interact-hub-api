using System.Text.Json.Serialization;

namespace InteractHub.Api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FriendshipStatus
{
    Pending = 1,  // Đang chờ xác nhận
    Accepted = 2, // Đã là bạn bè
    Blocked = 3   // Đã chặn 
}