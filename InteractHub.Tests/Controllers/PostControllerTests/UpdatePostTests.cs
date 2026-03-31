using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class UpdatePostTests : PostControllerTestBase
{
    // ==========================================
    // 1. HAPPY PATH (Đường màu hồng)
    // ==========================================
    [Fact]
    public async Task UpdatePost_AsOwner_ReturnsOkResult_WithUpdatedData()
    {
        // ARRANGE: Tạo bài viết gốc của chính mình
        var postId = Guid.NewGuid();
        var post = new Post
        {
            Id = postId,
            Content = "Nội dung cũ",
            UserId = _testUserId, // Tác giả là mình
            Visibility = PostVisibility.Private
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Chuẩn bị dữ liệu muốn sửa
        var updateRequest = new UpdatePostRequest
        {
            Content = "Nội dung đã được edit cực mạnh!",
            Visibility = PostVisibility.Public // Đổi luôn quyền riêng tư
        };

        // ACT: Gọi API
        var result = await _controller.UpdatePost(postId, updateRequest);

        // ASSERT: Xác nhận kết quả
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedResponse = Assert.IsType<PostResponse>(okResult.Value);

        // Kiểm tra DTO trả về xem có đúng dữ liệu mới không
        Assert.Equal("Nội dung đã được edit cực mạnh!", updatedResponse.Content);
        Assert.Equal(PostVisibility.Public, updatedResponse.Visibility);

        // Chọc thẳng vào DB ảo kiểm tra xem đã lưu thật chưa
        var postInDb = await _context.Posts.FindAsync(postId);
        Assert.Equal("Nội dung đã được edit cực mạnh!", postInDb!.Content);
        // Đảm bảo thời gian UpdatedAt đã được hệ thống tự động ghi nhận
        Assert.NotNull(postInDb.UpdatedAt);
    }

    // ==========================================
    // 2. NEGATIVE PATHS (Đường rủi ro)
    // ==========================================

    [Fact]
    public async Task UpdatePost_PostDoesNotExist_ReturnsNotFound()
    {
        // ARRANGE
        var fakePostId = Guid.NewGuid();
        var updateRequest = new UpdatePostRequest { Content = "Cố tình sửa bài ma" };

        // ACT
        var result = await _controller.UpdatePost(fakePostId, updateRequest);

        // ASSERT
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePost_UserIsNotAuthor_ReturnsForbidden()
    {
        // ARRANGE: Tạo bài viết của người khác
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var existingPost = new Post
        {
            Id = postId,
            Content = "Bài của người khác, đố bạn sửa được",
            UserId = anotherUserId,
            Visibility = PostVisibility.Public
        };
        _context.Posts.Add(existingPost);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdatePostRequest { Content = "Tôi là hacker, tôi muốn sửa bài này" };

        // ACT & ASSERT: Đổi thành kiểm tra Exception
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _controller.UpdatePost(postId, updateRequest)
        );
        Assert.Equal("You do not have permission to update this post.", exception.Message);

        // Đảm bảo DB không bị thay đổi
        var postInDb = await _context.Posts.FindAsync(postId);
        Assert.Equal("Bài của người khác, đố bạn sửa được", postInDb!.Content);
    }
}