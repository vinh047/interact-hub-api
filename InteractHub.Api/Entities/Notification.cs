using InteractHub.Api.Enums;

namespace InteractHub.Api.Entities;

public class Notification : BaseEntity
{
    // Người nhận thông báo
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Người tạo ra hành động (VD: Người bấm like, người comment) - Có thể Null nếu là thông báo hệ thống
    public Guid? IssuerId { get; set; }
    public ApplicationUser? Issuer { get; set; }

    public NotificationType Type { get; set; }
    public required string Content { get; set; }
    
    // Lưu ID của Bài viết, Comment hoặc Lời mời kết bạn để Frontend bấm vào chuyển trang
    public Guid? ReferenceId { get; set; } 
    public bool IsRead { get; set; } = false;
}