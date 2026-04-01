using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Xunit;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public class GetFriendsAsyncTests : FriendshipServiceTestBase
{
    [Fact]
    public async Task GetFriends_ReturnsOnlyAccepted_AndMapsCorrectUser()
    {
        // Arrange
        var friendId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        _context.Users.AddRange(
            new ApplicationUser { Id = friendId, FullName = "Bạn Tốt" },
            new ApplicationUser { Id = strangerId, FullName = "Người Dưng" }
        );

        // 1 người bạn (Đã chấp nhận)
        _context.Friendships.Add(new Friendship { RequesterId = _testUserId, AddresseeId = friendId, Status = FriendshipStatus.Accepted });
        
        // 1 người lạ (Đang chờ)
        _context.Friendships.Add(new Friendship { RequesterId = strangerId, AddresseeId = _testUserId, Status = FriendshipStatus.Pending });
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _friendshipService.GetFriendsAsync(new FriendshipParams(), _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Chỉ trả về 1 người bạn
        Assert.Equal(friendId, result[0].UserId); // Map ĐÚNG ID của người kia
        Assert.Equal("Bạn Tốt", result[0].FullName); // Map ĐÚNG Tên của người kia
    }
}