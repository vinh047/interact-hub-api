using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Xunit;

namespace InteractHub.Tests.Services.PostServiceTests;

public class GetNewsFeedAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task GetNewsFeedAsync_ReturnsAllEligiblePostsForUser()
    {
        // Arrange
        _context.Posts.AddRange(
            new Post { Id = Guid.NewGuid(), Content = "Public 1", UserId = _testUserId, Visibility = PostVisibility.Public },
            new Post { Id = Guid.NewGuid(), Content = "Private 1", UserId = _testUserId, Visibility = PostVisibility.Private }
        );
        await _context.SaveChangesAsync();
        var queryParams = new PostQueryParameters();

        // Act
        var result = await _postService.GetNewsFeedAsync(queryParams, _testUserId);

        // Assert
        Assert.Equal(2, result.Count); // Mong đợi lấy được cả bài Public và Private của chính mình
        
        // Kiểm tra xem trong list trả về có chứa 2 bài này không (không quan tâm thứ tự)
        Assert.Contains(result, p => p.Content == "Public 1");
        Assert.Contains(result, p => p.Content == "Private 1");
    }
}