using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Responses.Friendship;

public class FriendUserResponse
{
    // Thông tin của "Người kia" (Không cần biết là Requester hay Addressee)
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    // Trạng thái của mối quan hệ này
    public FriendshipStatus Status { get; set; }

    // Thời gian gửi yêu cầu hoặc thời gian trở thành bạn bè
    public DateTime CreatedAt { get; set; }
}