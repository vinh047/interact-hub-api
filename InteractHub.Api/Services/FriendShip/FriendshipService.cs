using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using InteractHub.Api.DTOs.Responses.Friendship;
using Microsoft.EntityFrameworkCore;
using InteractHub.Api.DTOs.Requests.Friendship;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Api.Hubs;
using InteractHub.Api.Events;
using MediatR;


namespace InteractHub.Api.Services;

public class FriendshipService(ApplicationDbContext context, IPublisher publisher) : IFriendshipService
{
    public async Task SendFriendRequestAsync(Guid targetUserId, Guid currentUserId)
    {
        if (currentUserId == targetUserId)
            throw new ArgumentException("You cannot send a friend request to yourself.");

        var targetUserExists = await context.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetUserExists)
            throw new KeyNotFoundException("The user you are trying to add does not exist.");

        var existingFriendship = await context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == targetUserId) ||
                (f.RequesterId == targetUserId && f.AddresseeId == currentUserId)
            );

        if (existingFriendship != null)
        {
            if (existingFriendship.Status == FriendshipStatus.Accepted)
                throw new InvalidOperationException("You are already friends with this user.");

            if (existingFriendship.Status == FriendshipStatus.Pending)
            {
                if (existingFriendship.RequesterId == currentUserId)
                    throw new InvalidOperationException("You have already sent a friend request to this user.");
                else
                    throw new InvalidOperationException("This user has already sent you a friend request. Please accept it.");
            }

            if (existingFriendship.Status == FriendshipStatus.Blocked)
                throw new UnauthorizedAccessException("You cannot interact with this user.");
        }

        var newFriendship = new Friendship
        {
            RequesterId = currentUserId,
            AddresseeId = targetUserId,
            Status = FriendshipStatus.Pending
        };

        context.Friendships.Add(newFriendship);
        await context.SaveChangesAsync();
        await publisher.Publish(new FriendRequestSentEvent(currentUserId, targetUserId));
    }

    public async Task AcceptFriendRequestAsync(Guid requesterId, Guid currentUserId)
    {
        var friendship = await context.Friendships
            .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == currentUserId);

        if (friendship == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new ArgumentException("This friend request has already been processed.");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<bool> RemoveFriendshipAsync(Guid otherUserId, Guid currentUserId)
    {
        var friendship = await context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == otherUserId) ||
                (f.RequesterId == otherUserId && f.AddresseeId == currentUserId)
            );

        if (friendship == null) return false;

        context.Friendships.Remove(friendship);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedList<FriendUserResponse>> GetFriendsAsync(FriendshipParams friendshipParams, Guid userId)
    {
        var query = context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterId == userId || f.AddresseeId == userId))
            .AsQueryable();

        // 1. Lọc theo tên nếu người dùng nhập ô search
        if (!string.IsNullOrWhiteSpace(friendshipParams.Search))
        {
            var search = friendshipParams.Search.ToLower();
            query = query.Where(f =>
                (f.RequesterId == userId && f.Addressee!.FullName.ToLower().Contains(search)) ||
                (f.AddresseeId == userId && f.Requester!.FullName.ToLower().Contains(search))
            );
        }

        var result = query.MapToFriendUserResponse(userId)
                          .OrderByDescending(f => f.CreatedAt);

        return await PagedList<FriendUserResponse>.CreateAsync(result, friendshipParams.Page, friendshipParams.Limit);
    }

    public async Task<PagedList<FriendUserResponse>> GetPendingRequestsAsync(FriendshipParams friendshipParams, Guid currentUserId)
    {
        var query = context.Friendships
            .Where(f => f.Status == FriendshipStatus.Pending && f.AddresseeId == currentUserId)
            .MapToFriendUserResponse(currentUserId) // Dùng Extension siêu gọn
            .OrderByDescending(f => f.CreatedAt);

        return await PagedList<FriendUserResponse>.CreateAsync(query, friendshipParams.Page, friendshipParams.Limit);
    }
}