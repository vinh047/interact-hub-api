using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Services.PostServiceTests;

public class DeletePostAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task DeletePostAsync_AsOwner_ReturnsTrue_AndSoftDeletes()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "To be deleted", UserId = _testUserId });
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.DeletePostAsync(postId, _testUserId);

        // Assert
        Assert.True(result);
        var postInDb = await _context.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId);
        Assert.True(postInDb!.IsDeleted);
    }

    [Fact]
    public async Task DeletePostAsync_UserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, Content = "Not mine", UserId = anotherUserId });
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _postService.DeletePostAsync(postId, _testUserId)
        );
        Assert.Equal("You do not have permission to delete this post.", exception.Message);
    }

    [Fact]
    public async Task DeletePostAsync_PostNotFound_ReturnsFalse()
    {
        // Act
        var result = await _postService.DeletePostAsync(Guid.NewGuid(), _testUserId);

        // Assert
        Assert.False(result); // Không tìm thấy thì trả về false (Controller sẽ hứng và ném 404)
    }
}