using InteractHub.Api.DTOs.Requests.Post;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Services.PostServiceTests;

public class CreatePostAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task CreatePostAsync_WithValidData_ReturnsPostResponse()
    {
        // Arrange
        var request = new CreatePostRequest { Content = "Test Service Create" };

        // Act
        var result = await _postService.CreatePostAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Service Create", result.Content);
        Assert.Equal(_testUserId, result.AuthorId);

        // Check DB
        var savedPost = await _context.Posts.FirstOrDefaultAsync();
        Assert.NotNull(savedPost);
        Assert.Equal("Test Service Create", savedPost.Content);
    }
}