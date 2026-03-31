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
    public async Task GetNewsFeed_ReturnsOkResult_WithPaginationAndOnlyPublicPosts()
    {
        // ARRANGE: Cài cắm dữ liệu vào DB ảo
        // Tạo 3 bài viết với thời gian và quyền riêng tư khác nhau
        var privatePost = new Post 
        { 
            Id = Guid.NewGuid(), Content = "Bài Private (Ẩn)", Visibility = PostVisibility.Private, 
            UserId = _testUserId, CreatedAt = DateTime.UtcNow.AddMinutes(-5) 
        };
        var publicOldPost = new Post 
        { 
            Id = Guid.NewGuid(), Content = "Bài Public Cũ", Visibility = PostVisibility.Public, 
            UserId = _testUserId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) 
        };
        var publicNewPost = new Post 
        { 
            Id = Guid.NewGuid(), Content = "Bài Public Mới", Visibility = PostVisibility.Public, 
            UserId = _testUserId, CreatedAt = DateTime.UtcNow.AddMinutes(-1) 
        };

        _context.Posts.AddRange(privatePost, publicOldPost, publicNewPost);
        await _context.SaveChangesAsync();

        var queryParams = new PostQueryParameters { PageNumber = 1, PageSize = 10 };

        // ACT: Gọi API
        var result = await _controller.GetNewsFeed(queryParams);

        // ASSERT: Xác nhận sự thật
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Dữ liệu trả về phải là một danh sách các PostResponse
        var returnedPosts = Assert.IsAssignableFrom<IEnumerable<PostResponse>>(okResult.Value).ToList();

        // 1. TEST BỘ LỌC: Chỉ có 2 bài Public được lọt qua (Bài Private bị chặn lại)
        Assert.Equal(2, returnedPosts.Count);

        // 2. TEST SẮP XẾP: Bài "Mới" phải nằm trên cùng (Index 0), bài "Cũ" nằm dưới
        Assert.Equal("Bài Public Mới", returnedPosts[0].Content);
        Assert.Equal("Bài Public Cũ", returnedPosts[1].Content);

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
        var result = await _controller.GetNewsFeed(queryParams);

        // ASSERT: Phải bị đá ra bằng mã 401
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }
}