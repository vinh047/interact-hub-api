using InteractHub.Api.DTOs.Responses.Friendship;
using InteractHub.Api.Entities;

namespace InteractHub.Api.Extensions;

public static class FriendshipExtensions
{
    // Truyền currentUserId vào để biết ai là "Mình", ai là "Người kia"
    public static IQueryable<FriendUserResponse> MapToFriendUserResponse(this IQueryable<Friendship> query, Guid currentUserId)
    {
        return query.Select(f => new FriendUserResponse
        {
            // Nếu mình là người gửi -> Lấy info người nhận. Ngược lại -> Lấy info người gửi
            UserId = f.RequesterId == currentUserId ? f.Addressee!.Id : f.Requester!.Id,
            FullName = f.RequesterId == currentUserId ? f.Addressee!.FullName : f.Requester!.FullName,
            AvatarUrl = f.RequesterId == currentUserId ? f.Addressee!.AvatarUrl : f.Requester!.AvatarUrl,
            Bio = f.RequesterId == currentUserId ? f.Addressee!.Bio : f.Requester!.Bio,
            
            Status = f.Status,
            CreatedAt = f.UpdatedAt ?? f.CreatedAt // Trả về thời gian cập nhật mới nhất
        });
    }
}