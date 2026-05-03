using System.Security.Claims;
using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class GetNewsFeedTests : PostControllerTestBase
{
    // ==========================================
    // 1. HAPPY PATH (Test trọn gói Lọc, Sắp xếp, Phân trang và Header)
    // ==========================================
    [Fact]
    public async Task GetNewsFeed_ReturnsOkResult_WithPaginationAndEligiblePosts()
    {
        // ARRANGE: Cài cắm dữ liệu vào DB ảo
        // Tạo 3 bài viết với thời gian và quyền riêng tư khác nhau
        var privatePost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Bài Private (Ẩn)",
            Visibility = PostVisibility.Private,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var publicOldPost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Bài Public Cũ",
            Visibility = PostVisibility.Public,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var publicNewPost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Bài Public Mới",
            Visibility = PostVisibility.Public,
            UserId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _context.Posts.AddRange(privatePost, publicOldPost, publicNewPost);
        await _context.SaveChangesAsync();

        var queryParams = new PostQueryParameters { Page = 1, Limit = 10 };

        // ACT: Gọi API
        var result = await _controller.GetNewsFeed(queryParams);

        // ASSERT: Xác nhận sự thật
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Dữ liệu trả về phải là một danh sách các PostResponse
        var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostResponse>>(okResult.Value).ToList();

        // 1. TEST BỘ LỌC: Phải lấy được cả 3 bài (do bài Private là của chính user này)
        Assert.Equal(3, returnedPosts.Count);

        // 2. TEST NỘI DUNG VÀ SẮP XẾP: 
        // Vì bài mới nhất là "Public Mới" (AddMinutes(-1)), nó phải ở đầu.
        Assert.Equal("Bài Public Mới", returnedPosts[0].Content);
        
        // Dùng Assert.Contains cho 2 bài còn lại để tránh lỗi xô lệch vị trí
        Assert.Contains(returnedPosts, p => p.Content == "Bài Public Cũ");
        Assert.Contains(returnedPosts, p => p.Content == "Bài Private (Ẩn)");

        // 3. TEST HEADER PHÂN TRANG: Đảm bảo Extension Method "AddPaginationHeader" đã chạy thành công
        var paginationHeader = _controller.Response.Headers["X-Pagination"];
        Assert.True(paginationHeader.Count > 0, "Header X-Pagination không được trống!");
    }

    // ==========================================
    // 2. NEGATIVE PATH (Đường rủi ro)
    // ==========================================
    [Fact]
    public async Task GetNewsFeed_UserNotAuthenticated_ReturnsUnauthorized()
    {
        // ARRANGE: Hack HttpContext, tước quyền đăng nhập
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())
        }, "mock_auth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = unauthenticatedUser }
        };

        var queryParams = new PostQueryParameters();

        // ACT
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => _controller.GetNewsFeed(new PostQueryParameters())
    );
        Assert.Equal("User identity not found in token.", exception.Message);
    }
}