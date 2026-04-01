using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Xunit;

namespace InteractHub.Tests.Services.StoryServiceTests;

public class GetStoryFeedAsyncTests : StoryServiceTestBase
{
    [Fact]
    public async Task GetStoryFeed_ReturnsOwnStories_AndAcceptedFriendsStories()
    {
        // Arrange
        var friendId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        // Nạp User
        _context.Users.AddRange(
            new ApplicationUser { Id = friendId, FullName = "Bạn Thân" },
            new ApplicationUser { Id = strangerId, FullName = "Người Lạ" }
        );

        // Tạo quan hệ bạn bè (Accepted)
        _context.Friendships.Add(new Friendship 
        { 
            RequesterId = _testUserId, AddresseeId = friendId, Status = FriendshipStatus.Accepted 
        });

        // Tạo 3 cái Story: Phải nhớ cho thêm ExpiresAt vào ngày mai để không bị EF Core lọc mất!
        _context.Stories.AddRange(
            new Story { Id = Guid.NewGuid(), UserId = _testUserId, MediaUrl = "my-story.jpg", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new Story { Id = Guid.NewGuid(), UserId = friendId, MediaUrl = "friend-story.jpg", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new Story { Id = Guid.NewGuid(), UserId = strangerId, MediaUrl = "stranger-story.jpg", ExpiresAt = DateTime.UtcNow.AddDays(1) }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _storyService.GetStoryFeedAsync(new StoryParams(), _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Bây giờ chắc chắn sẽ lấy được 2 cái!
        Assert.DoesNotContain(result, s => s.AuthorId == strangerId); 
    }
}