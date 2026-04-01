using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.StoryServiceTests;

public class DeleteStoryAsyncTests : StoryServiceTestBase
{
    [Fact]
    public async Task DeleteStory_NotOwner_ThrowsUnauthorized()
    {
        var storyId = Guid.NewGuid();
        _context.Stories.Add(new Story { Id = storyId, UserId = Guid.NewGuid(), MediaUrl = "lạ.jpg" });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _storyService.DeleteStoryAsync(storyId, _testUserId)
        );
        Assert.Equal("You do not have permission to delete this story.", exception.Message);
    }

    [Fact]
    public async Task DeleteStory_AsOwner_ReturnsTrue()
    {
        var storyId = Guid.NewGuid();
        _context.Stories.Add(new Story { Id = storyId, UserId = _testUserId, MediaUrl = "quen.jpg" });
        await _context.SaveChangesAsync();

        var result = await _storyService.DeleteStoryAsync(storyId, _testUserId);

        Assert.True(result);
        var storyInDb = await _context.Stories.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == storyId);
        
        // Phải đảm bảo nó trả về Null (Bốc hơi hoàn toàn khỏi Database)
        Assert.Null(storyInDb);
    }
}