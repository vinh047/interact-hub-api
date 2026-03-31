using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class GetUserPostsTests : PostControllerTestBase
{
    [Fact]
    public async Task GetUserPosts_ViewingOwnProfile_CanSeePrivatePosts()
    {
        // ARRANGE: Mình có 1 bài Public, 1 bài Private
        _context.Posts.AddRange(
            new Post { Id = Guid.NewGuid(), Content = "Public", UserId = _testUserId, Visibility = PostVisibility.Public },
            new Post { Id = Guid.NewGuid(), Content = "Private", UserId = _testUserId, Visibility = PostVisibility.Private }
        );
        await _context.SaveChangesAsync();

        var queryParams = new PostQueryParameters();

        // ACT: Lấy danh sách bài của CHÍNH MÌNH
        var result = await _controller.GetUserPosts(_testUserId, queryParams);

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostResponse>>(okResult.Value).ToList();

        // Phải lấy được cả 2 bài
        Assert.Equal(2, returnedPosts.Count);
    }

    [Fact]
    public async Task GetUserPosts_ViewingOtherProfile_OnlySeesPublicPosts()
    {
        // ARRANGE: 1. BẮT BUỘC PHẢI TẠO USER LẠ TRƯỚC
        var otherUserId = Guid.NewGuid();

        _context.Users.Add(new ApplicationUser
        {
            Id = otherUserId,
            FullName = "Người Lạ Xem Chùa",
            Email = "nguoila@test.com",
            PasswordHash = "123"
        });

        // 2. SAU ĐÓ MỚI TẠO BÀI VIẾT CHO USER NÀY
        _context.Posts.AddRange(
            new Post { Id = Guid.NewGuid(), Content = "Public của bạn", UserId = otherUserId, Visibility = PostVisibility.Public },
            new Post { Id = Guid.NewGuid(), Content = "Private của bạn", UserId = otherUserId, Visibility = PostVisibility.Private }
        );
        await _context.SaveChangesAsync();

        var queryParams = new PostQueryParameters();

        // ACT: Mình (đang là _testUserId) vào trang cá nhân của người khác (otherUserId)
        var result = await _controller.GetUserPosts(otherUserId, queryParams);

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostResponse>>(okResult.Value).ToList();

        // Chỉ được phép thấy đúng 1 bài Public
        Assert.Single(returnedPosts);
        Assert.Equal(PostVisibility.Public, returnedPosts[0].Visibility);
    }
}