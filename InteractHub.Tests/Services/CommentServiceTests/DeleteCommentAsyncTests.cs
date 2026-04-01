using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Services.CommentServiceTests;

public class DeleteCommentAsyncTests : CommentServiceTestBase
{
    [Fact]
    public async Task DeleteCommentAsync_CommentNotFound_ReturnsFalse()
    {
        var result = await _commentService.DeleteCommentAsync(Guid.NewGuid(), _testUserId);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCommentAsync_NeitherOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Comment của người khác, nằm trong Bài viết của người khác
        var commentId = Guid.NewGuid();
        var anotherUser1 = Guid.NewGuid();
        var anotherUser2 = Guid.NewGuid();

        var post = new Post { Id = Guid.NewGuid(), UserId = anotherUser1, Content = "Bài của người lạ" };
        _context.Posts.Add(post);
        _context.Comments.Add(new Comment { Id = commentId, PostId = post.Id, UserId = anotherUser2, Content = "Cmt của người lạ" });
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _commentService.DeleteCommentAsync(commentId, _testUserId)
        );
        Assert.Equal("You do not have permission to delete this comment.", exception.Message);
    }

    [Fact]
    public async Task DeleteCommentAsync_AsCommentOwner_ReturnsTrue()
    {
        // Arrange: Comment của mình (bất kể bài của ai)
        var postId = Guid.NewGuid(); // <-- TẠO BIẾN LƯU ID BÀI VIẾT
        var commentId = Guid.NewGuid();
        
        // Gắn ID cho bài viết
        _context.Posts.Add(new Post { Id = postId, UserId = Guid.NewGuid(), Content = "Bài viết gốc" }); 
        
        // GẮN POSTID CHO COMMENT ĐỂ CHÚNG LIÊN KẾT VỚI NHAU
        _context.Comments.Add(new Comment { Id = commentId, PostId = postId, UserId = _testUserId, Content = "Cmt của tôi" }); 
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _commentService.DeleteCommentAsync(commentId, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCommentAsync_AsPostOwner_ReturnsTrue()
    {
        // Arrange: Bài của mình, nhưng Comment của người khác
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        
        _context.Posts.Add(new Post { Id = postId, UserId = _testUserId, Content = "Bài viết gốc" }); // Bài của mình
        _context.Comments.Add(new Comment { Id = commentId, PostId = postId, UserId = Guid.NewGuid(), Content = "Cmt của ai đó dạo qua" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _commentService.DeleteCommentAsync(commentId, _testUserId);

        // Assert
        Assert.True(result); // Chủ bài viết có quyền xóa comment dạo!
    }
}