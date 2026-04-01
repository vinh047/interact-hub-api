using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.CommentServiceTests;

public class UpdateCommentAsyncTests : CommentServiceTestBase
{
    [Fact]
    public async Task UpdateCommentAsync_CommentNotFound_ReturnsNull()
    {
        // Act
        var result = await _commentService.UpdateCommentAsync(Guid.NewGuid(), new UpdateCommentRequest { Content = "Cập nhật" }, _testUserId);

        // Assert
        Assert.Null(result); // Service trả null để Controller biết đường báo 404
    }

    [Fact]
    public async Task UpdateCommentAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        _context.Comments.Add(new Comment { Id = commentId, UserId = anotherUserId, Content = "Cmt lạ" });
        await _context.SaveChangesAsync();

        var request = new UpdateCommentRequest { Content = "Cố tình sửa" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _commentService.UpdateCommentAsync(commentId, request, _testUserId)
        );
        Assert.Equal("You do not have permission to edit this comment.", exception.Message);
    }

    [Fact]
    public async Task UpdateCommentAsync_AsOwner_ReturnsUpdatedComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        _context.Comments.Add(new Comment { Id = commentId, UserId = _testUserId, Content = "Cũ" });
        await _context.SaveChangesAsync();

        var request = new UpdateCommentRequest { Content = "Mới" };

        // Act
        var result = await _commentService.UpdateCommentAsync(commentId, request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Mới", result.Content);

        var dbComment = await _context.Comments.FindAsync(commentId);
        Assert.Equal("Mới", dbComment!.Content);
        Assert.NotNull(dbComment.UpdatedAt);
    }
}