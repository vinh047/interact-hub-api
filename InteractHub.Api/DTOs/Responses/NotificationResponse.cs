// DTOs/Responses/NotificationResponse.cs
using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Responses;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public required string Content { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Thêm thông tin người tương tác
    public Guid? IssuerId { get; set; }
    public string? IssuerName { get; set; }
    public string? IssuerAvatar { get; set; }
}