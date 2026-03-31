using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.Entities;

namespace InteractHub.Tests.Services.PostServiceTests;

public class UpdatePostAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task UpdatePostAsync_AsOwner_ReturnsUpdatedPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "Cũ", UserId = _testUserId });
        await _context.SaveChangesAsync();

        var request = new UpdatePostRequest { Content = "Mới" };

        // Act
        var result = await _postService.UpdatePostAsync(postId, request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Mới", result.Content);

        var postInDb = await _context.Posts.FindAsync(postId);
        Assert.Equal("Mới", postInDb!.Content);
    }

    [Fact]
    public async Task UpdatePostAsync_UserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "Not mine", UserId = anotherUserId });
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _postService.UpdatePostAsync(postId, new UpdatePostRequest { Content = "Hack" }, _testUserId)
        );
    }
}