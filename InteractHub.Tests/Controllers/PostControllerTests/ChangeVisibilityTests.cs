using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class ChangeVisibilityTests : PostControllerTestBase
{
    [Fact]
    public async Task ChangeVisibility_AsOwner_ReturnsOkResult_WithUpdatedVisibility()
    {
        // ARRANGE
        var postId = Guid.NewGuid();
        var post = new Post
        {
            Id = postId,
            Content = "Bài này lúc đầu là Private",
            UserId = _testUserId,
            Visibility = PostVisibility.Private
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // ACT: Đổi thành Public
        var result = await _controller.ChangeVisibility(postId, PostVisibility.Public);

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedResponse = Assert.IsType<PostResponse>(okResult.Value);

        Assert.Equal(PostVisibility.Public, updatedResponse.Visibility);

        // Check DB ảo
        var postInDb = await _context.Posts.FindAsync(postId);
        Assert.Equal(PostVisibility.Public, postInDb!.Visibility);
    }

    [Fact]
    public async Task ChangeVisibility_UserIsNotAuthor_ReturnsForbidden()
    {
        // ARRANGE
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        
        _context.Posts.Add(new Post
        {
            Id = postId, Content = "Bài người khác", UserId = anotherUserId, Visibility = PostVisibility.Public
        });
        await _context.SaveChangesAsync();

        // ACT & ASSERT: Đổi thành kiểm tra Exception
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _controller.ChangeVisibility(postId, PostVisibility.Private)
        );
        Assert.Equal("You do not have permission to modify this post.", exception.Message);
    }
}