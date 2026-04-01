using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.CommentServiceTests;

public class CreateCommentAsyncTests : CommentServiceTestBase
{
    [Fact]
    public async Task CreateCommentAsync_PostNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new CreateCommentRequest { PostId = Guid.NewGuid(), Content = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _commentService.CreateCommentAsync(request, _testUserId)
        );
        Assert.Equal("The post you are trying to comment on does not exist or has been deleted.", exception.Message);
    }

    [Fact]
    public async Task CreateCommentAsync_ParentCommentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, UserId = _testUserId, Content = "Bài viết gốc" });
        await _context.SaveChangesAsync();

        var request = new CreateCommentRequest { PostId = postId, ParentCommentId = Guid.NewGuid(), Content = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _commentService.CreateCommentAsync(request, _testUserId)
        );
        Assert.Equal("The parent comment does not exist in this post.", exception.Message);
    }

    [Fact]
    public async Task CreateCommentAsync_ValidData_ReturnsCommentResponse()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, UserId = _testUserId, Content = "Bài viết gốc" });
        await _context.SaveChangesAsync();

        var request = new CreateCommentRequest { PostId = postId, Content = "Bình luận đầu tiên!" };

        // Act
        var result = await _commentService.CreateCommentAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bình luận đầu tiên!", result.Content);
        Assert.Equal(postId, result.PostId);

        // Verify DB
        var savedComment = await _context.Comments.FirstOrDefaultAsync();
        Assert.NotNull(savedComment);
    }
}