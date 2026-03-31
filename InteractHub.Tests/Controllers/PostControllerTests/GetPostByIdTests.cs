using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class GetPostByIdTests : PostControllerTestBase
{
    [Fact]
    public async Task GetPostById_PublicPost_ReturnsOkResult()
    {
        // ARRANGE: Tạo 1 thằng User lạ và lưu vào DB ảo TRƯỚC
        var postId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _context.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            FullName = "Người Lạ Ơi",
            Email = "nguoila@test.com",
            PasswordHash = "hash123"
        });

        // BÂY GIỜ mới lưu bài viết của thằng User lạ đó
        _context.Posts.Add(new Post
        {
            Id = postId,
            Content = "Hello",
            UserId = otherUserId,
            Visibility = PostVisibility.Public
        });
        await _context.SaveChangesAsync();

        // ACT
        var result = await _controller.GetPostById(postId);

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PostResponse>(okResult.Value);
        Assert.Equal(postId, response.Id);
    }

    [Fact]
    public async Task GetPostById_OwnPrivatePost_ReturnsOkResult()
    {
        // (Bài của mình thì không bị lỗi vì _testUserId đã được tạo sẵn ở BaseClass rồi)
        var postId = Guid.NewGuid();
        _context.Posts.Add(new Post
        {
            Id = postId,
            Content = "Bí mật của tôi",
            UserId = _testUserId,
            Visibility = PostVisibility.Private
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetPostById(postId);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPostById_OtherUserPrivatePost_ReturnsNotFound()
    {
        // ARRANGE: Tương tự, tạo User lạ trước
        var postId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _context.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            FullName = "Hacker",
            Email = "hacker@test.com",
            PasswordHash = "hash"
        });

        _context.Posts.Add(new Post
        {
            Id = postId,
            Content = "Bí mật của người khác",
            UserId = otherUserId,
            Visibility = PostVisibility.Private
        });
        await _context.SaveChangesAsync();

        // ACT
        var result = await _controller.GetPostById(postId);

        // ASSERT
        Assert.IsType<NotFoundObjectResult>(result);
    }
}