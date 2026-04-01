using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.Entities;
using Xunit;

namespace InteractHub.Tests.Services.StoryServiceTests;

public class GetStoryArchiveAsyncTests : StoryServiceTestBase
{
    [Fact]
    public async Task GetStoryArchive_ReturnsOnlyMyStories_IncludingExpired()
    {
        var anotherUserId = Guid.NewGuid();
        _context.Users.Add(new ApplicationUser { Id = anotherUserId, FullName = "Ai Đó" });

        _context.Stories.AddRange(
            new Story { Id = Guid.NewGuid(), UserId = _testUserId, MediaUrl = "kho-1.jpg" },
            new Story { Id = Guid.NewGuid(), UserId = _testUserId, MediaUrl = "kho-2.jpg" },
            new Story { Id = Guid.NewGuid(), UserId = anotherUserId, MediaUrl = "kho-lạ.jpg" }
        );
        await _context.SaveChangesAsync();

        var result = await _storyService.GetStoryArchiveAsync(new StoryParams(), _testUserId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(_testUserId, s.AuthorId));
    }
}