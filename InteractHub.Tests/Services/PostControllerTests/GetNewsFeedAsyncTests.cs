using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;

namespace InteractHub.Tests.Services.PostServiceTests;

public class GetNewsFeedAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task GetNewsFeedAsync_ReturnsOnlyPublicPosts()
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
        Assert.Single(result); // Chỉ mong đợi 1 bài public lọt qua
        Assert.Equal("Public 1", result[0].Content);
    }
}