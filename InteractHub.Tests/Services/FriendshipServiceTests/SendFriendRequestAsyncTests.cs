using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public class SendFriendRequestAsyncTests : FriendshipServiceTestBase
{
    [Fact]
    public async Task SendRequest_ToSelf_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _friendshipService.SendFriendRequestAsync(_testUserId, _testUserId)
        );
        Assert.Equal("You cannot send a friend request to yourself.", exception.Message);
    }

    [Fact]
    public async Task SendRequest_TargetUserNotFound_ThrowsKeyNotFoundException()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _friendshipService.SendFriendRequestAsync(Guid.NewGuid(), _testUserId)
        );
        Assert.Equal("The user you are trying to add does not exist.", exception.Message);
    }

    [Fact]
    public async Task SendRequest_AlreadyFriends_ThrowsInvalidOperationException()
    {
        var targetId = Guid.NewGuid();
        _context.Users.Add(new ApplicationUser { Id = targetId, FullName = "Người Dưng" });
        _context.Friendships.Add(new Friendship { RequesterId = _testUserId, AddresseeId = targetId, Status = FriendshipStatus.Accepted });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _friendshipService.SendFriendRequestAsync(targetId, _testUserId)
        );
        Assert.Equal("You are already friends with this user.", exception.Message);
    }

    [Fact]
    public async Task SendRequest_Valid_CreatesPendingFriendship()
    {
        var targetId = Guid.NewGuid();
        _context.Users.Add(new ApplicationUser { Id = targetId, FullName = "Người Dưng" });
        await _context.SaveChangesAsync();

        await _friendshipService.SendFriendRequestAsync(targetId, _testUserId);

        var friendship = await _context.Friendships.FirstOrDefaultAsync();
        Assert.NotNull(friendship);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        Assert.Equal(_testUserId, friendship.RequesterId);
        Assert.Equal(targetId, friendship.AddresseeId);
    }
}