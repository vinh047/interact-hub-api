using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.LikeServiceTests;

public class ToggleLikeAsyncTests : LikeServiceTestBase
{
    [Fact]
    public async Task ToggleLikeAsync_PostNotFound_ThrowsKeyNotFoundException()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _likeService.ToggleLikeAsync(Guid.NewGuid(), _testUserId)
        );
        Assert.Equal("Post not found or has been deleted.", exception.Message);
    }

    [Fact]
    public async Task ToggleLikeAsync_FirstTime_AddsLike_ReturnsTrue()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, UserId = Guid.NewGuid(), Content = "Test Post" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _likeService.ToggleLikeAsync(postId, _testUserId);

        // Assert
        Assert.True(result); // Trả về true (Đã Like)
        var likeInDb = await _context.Likes.FirstOrDefaultAsync();
        Assert.NotNull(likeInDb);
    }

    [Fact]
    public async Task ToggleLikeAsync_AlreadyLiked_RemovesLike_ReturnsFalse()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, UserId = Guid.NewGuid(), Content = "Test Post" });
        _context.Likes.Add(new Like { PostId = postId, UserId = _testUserId });
        await _context.SaveChangesAsync();

        // Act
        var result = await _likeService.ToggleLikeAsync(postId, _testUserId);

        // Assert
        Assert.False(result); // Trả về false (Đã Unlike)
        var likeInDb = await _context.Likes.FirstOrDefaultAsync();
        Assert.Null(likeInDb); // Dữ liệu bị xóa thật
    }
}