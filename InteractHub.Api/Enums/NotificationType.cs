using System.Text.Json.Serialization;

namespace InteractHub.Api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    Like,
    Comment,
    FriendRequest,
    FriendAccept,
    System
}