using System.Text.Json.Serialization;

namespace InteractHub.Api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostVisibility
{
    Public = 0,
    FriendsOnly = 1,
    Private = 2
}