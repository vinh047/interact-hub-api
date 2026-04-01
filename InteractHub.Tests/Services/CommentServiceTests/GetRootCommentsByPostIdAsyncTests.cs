using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.Entities;
using Xunit;

namespace InteractHub.Tests.Services.CommentServiceTests;

public class GetRootCommentsByPostIdAsyncTests : CommentServiceTestBase
{
    [Fact]
    public async Task GetRootComments_PostNotFound_ReturnsNull()
    {
        var result = await _commentService.GetRootCommentsByPostIdAsync(Guid.NewGuid(), new CommentParams());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRootComments_Success_ReturnsOnlyRootComments()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post { Id = postId, UserId = _testUserId, Content = "Bài viết gốc" });
        
        // Tạo 1 Root Comment và 1 Reply Comment
        var rootCommentId = Guid.NewGuid();
        _context.Comments.AddRange(
            new Comment { Id = rootCommentId, PostId = postId, UserId = _testUserId, Content = "Gốc" },
            new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = _testUserId, ParentCommentId = rootCommentId, Content = "Trả lời" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _commentService.GetRootCommentsByPostIdAsync(postId, new CommentParams());

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Chỉ mong đợi 1 cái (vì cái kia là Reply)
        Assert.Equal("Gốc", result[0].Content);
    }
}