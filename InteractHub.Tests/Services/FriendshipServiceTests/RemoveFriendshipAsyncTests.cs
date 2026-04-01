using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public class RemoveFriendshipAsyncTests : FriendshipServiceTestBase
{
    [Fact]
    public async Task RemoveFriendship_NotFound_ReturnsFalse()
    {
        var result = await _friendshipService.RemoveFriendshipAsync(Guid.NewGuid(), _testUserId);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveFriendship_Exists_RemovesAndReturnsTrue()
    {
        var otherUserId = Guid.NewGuid();
        _context.Friendships.Add(new Friendship 
        { 
            RequesterId = _testUserId, AddresseeId = otherUserId, Status = FriendshipStatus.Accepted 
        });
        await _context.SaveChangesAsync();

        var result = await _friendshipService.RemoveFriendshipAsync(otherUserId, _testUserId);

        Assert.True(result);
        var inDb = await _context.Friendships.FirstOrDefaultAsync();
        Assert.Null(inDb); // Xóa cứng thành công!
    }
}