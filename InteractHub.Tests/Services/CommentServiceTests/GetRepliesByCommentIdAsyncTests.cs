using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.Entities;
using Xunit;

namespace InteractHub.Tests.Services.CommentServiceTests;

public class GetRepliesByCommentIdAsyncTests : CommentServiceTestBase
{
    [Fact]
    public async Task GetReplies_CommentNotFound_ReturnsNull()
    {
        var result = await _commentService.GetRepliesByCommentIdAsync(Guid.NewGuid(), new CommentParams());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetReplies_Success_ReturnsReplies()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var rootCommentId = Guid.NewGuid();
        
        _context.Posts.Add(new Post { Id = postId, UserId = _testUserId, Content = "Bài viết gốc" });
        _context.Comments.AddRange(
            new Comment { Id = rootCommentId, PostId = postId, UserId = _testUserId, Content = "Gốc" },
            new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = _testUserId, ParentCommentId = rootCommentId, Content = "Trả lời 1" },
            new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = _testUserId, ParentCommentId = rootCommentId, Content = "Trả lời 2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _commentService.GetRepliesByCommentIdAsync(rootCommentId, new CommentParams());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Mong đợi 2 cái reply
    }
}