using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public class AcceptFriendRequestAsyncTests : FriendshipServiceTestBase
{
    [Fact]
    public async Task AcceptRequest_NotFound_ThrowsKeyNotFoundException()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _friendshipService.AcceptFriendRequestAsync(Guid.NewGuid(), _testUserId)
        );
        Assert.Equal("Friend request not found.", exception.Message);
    }

    [Fact]
    public async Task AcceptRequest_Valid_ChangesStatusToAccepted()
    {
        var requesterId = Guid.NewGuid();
        _context.Friendships.Add(new Friendship 
        { 
            RequesterId = requesterId, 
            AddresseeId = _testUserId, 
            Status = FriendshipStatus.Pending 
        });
        await _context.SaveChangesAsync();

        await _friendshipService.AcceptFriendRequestAsync(requesterId, _testUserId);

        var friendship = await _context.Friendships.FirstAsync();
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.NotNull(friendship.UpdatedAt);
    }
}