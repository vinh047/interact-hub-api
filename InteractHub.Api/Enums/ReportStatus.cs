using System.Text.Json.Serialization;

namespace InteractHub.Api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Pending,
    Reviewed,
    Dismissed
}