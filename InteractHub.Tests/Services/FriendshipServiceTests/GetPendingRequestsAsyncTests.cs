using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Xunit;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public class GetPendingRequestsAsyncTests : FriendshipServiceTestBase
{
    [Fact]
    public async Task GetPendingRequests_ReturnsOnlyPending_AddressedToMe()
    {
        var sender1 = Guid.NewGuid();
        var sender2 = Guid.NewGuid();

        _context.Users.AddRange(
            new ApplicationUser { Id = sender1, FullName = "Người gửi 1" },
            new ApplicationUser { Id = sender2, FullName = "Người gửi 2" }
        );

        // Gửi cho mình (Pending) -> Phải lấy
        _context.Friendships.Add(new Friendship { RequesterId = sender1, AddresseeId = _testUserId, Status = FriendshipStatus.Pending });
        
        // Mình gửi cho người ta (Pending) -> KHÔNG ĐƯỢC LẤY
        _context.Friendships.Add(new Friendship { RequesterId = _testUserId, AddresseeId = sender2, Status = FriendshipStatus.Pending });
        
        await _context.SaveChangesAsync();

        var result = await _friendshipService.GetPendingRequestsAsync(new FriendshipParams(), _testUserId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(sender1, result[0].UserId); // Chỉ lấy cái người ta gửi cho mình
    }
}