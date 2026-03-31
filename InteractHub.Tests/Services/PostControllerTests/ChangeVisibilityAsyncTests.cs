using InteractHub.Api.Entities;
using InteractHub.Api.Enums;

namespace InteractHub.Tests.Services.PostServiceTests;

public class ChangeVisibilityAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task ChangeVisibilityAsync_AsOwner_ReturnsUpdatedPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "Content", UserId = _testUserId, Visibility = PostVisibility.Private });
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.ChangeVisibilityAsync(postId, PostVisibility.Public, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PostVisibility.Public, result.Visibility);
    }

    [Fact]
    public async Task ChangeVisibilityAsync_UserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "Not mine", UserId = anotherUserId });
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _postService.ChangeVisibilityAsync(postId, PostVisibility.Public, _testUserId)
        );
    }
}