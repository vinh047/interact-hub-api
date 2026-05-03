using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.DTOs.Responses.Friendship;
using InteractHub.Api.Helpers;

namespace InteractHub.Api.Services;

public interface IFriendshipService
{
    Task SendFriendRequestAsync(Guid targetUserId, Guid currentUserId);
    Task AcceptFriendRequestAsync(Guid requesterId, Guid currentUserId);
    Task<bool> RemoveFriendshipAsync(Guid otherUserId, Guid currentUserId);
    Task<PagedList<FriendUserResponse>> GetFriendsAsync(FriendshipParams friendshipParams, Guid userId);
    Task<PagedList<FriendUserResponse>> GetPendingRequestsAsync(FriendshipParams friendshipParams, Guid currentUserId);
}