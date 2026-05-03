using System.Security.Claims;
using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class CreatePostTests : PostControllerTestBase // <-- Kế thừa ở đây
{

    [Fact] // Đánh dấu đây là 1 Test Case cho xUnit biết
    public async Task CreatePost_WithValidTextOnly_ReturnsOkResult()
    {
        // 1. ARRANGE: Chuẩn bị dữ liệu gửi lên
        var request = new CreatePostRequest
        {
            Content = "Hôm nay là ngày tôi viết Unit Test đầu tiên!",
            MediaFiles = null // Không có ảnh
        };

        // 2. ACT: Bấm nút "Gửi" (Gọi hàm Controller)
        var result = await _controller.CreatePost(request);

        // 3. ASSERT: Kiểm tra sự thật

        // A. Kiểm tra API có trả về HTTP 200 (OkObjectResult) không?
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // B. Kiểm tra Database Ảo xem bài viết đã thực sự được lưu vào chưa?
        var savedPost = await _context.Posts.FirstOrDefaultAsync();
        Assert.NotNull(savedPost); // Chắc chắn là có lưu
        Assert.Equal("Hôm nay là ngày tôi viết Unit Test đầu tiên!", savedPost.Content); // Nội dung phải khớp
        Assert.Equal(_testUserId, savedPost.UserId); // ID người đăng phải là mình
        Assert.Empty(savedPost.MediaFiles); // Không có ảnh
    }

    [Fact]
    public async Task CreatePost_WithMediaFiles_ReturnsOkResult_AndSavesMedia()
    {
        // ==========================================
        // 1. ARRANGE (Chuẩn bị hiện trường giả)
        // ==========================================

        // A. Nặn ra một cái File ảo (Mock IFormFile) bằng không khí
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test-avatar.jpg");
        mockFile.Setup(f => f.Length).Returns(1024); // Nặng 1KB
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg"); // Báo đây là hình ảnh

        // B. RA LỆNH CHO IFileService NÓI DỐI:
        // "Ê IFileService, lát nữa Controller có nhờ mầy upload bất kỳ cái file nào (It.IsAny), 
        // thì mầy không được lưu thật, cứ trả về cái link ảo này cho tui!"
        _mockFileService
            .Setup(service => service.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("https://interacthub.local/uploads/test-avatar.jpg");

        // C. Gói hàng gửi lên Controller
        var request = new CreatePostRequest
        {
            Content = "Hello thế giới, đây là bài test có kèm ảnh!",
            MediaFiles = new List<IFormFile> { mockFile.Object } // Nhét cái file ảo vào
        };

        // ==========================================
        // 2. ACT (Hành động)
        // ==========================================
        var result = await _controller.CreatePost(request);

        // ==========================================
        // 3. ASSERT (Kiểm tra lời nói dối có lọt lưới không)
        // ==========================================

        // Trả về 200 OK
        Assert.IsType<OkObjectResult>(result);

        // Chọc vào DB ảo lấy bài viết ra xem
        var savedPost = await _context.Posts
            .Include(p => p.MediaFiles) // Nhớ Include để lấy danh sách ảnh
            .FirstOrDefaultAsync(p => p.Content!.Contains("có kèm ảnh"));

        Assert.NotNull(savedPost);

        // Phải có đúng 1 tấm ảnh trong Database
        Assert.Single(savedPost.MediaFiles);

        var savedMedia = savedPost.MediaFiles.First();

        // Link lưu trong DB phải đúng là cái link ảo mà mình ép IFileService nhả ra lúc nãy
        Assert.Equal("https://interacthub.local/uploads/test-avatar.jpg", savedMedia.MediaUrl);

        // Hệ thống phải nhận diện đúng nó là Image (nhờ ContentType mình set là image/jpeg)
        Assert.Equal(MediaType.Image, savedMedia.MediaType);
    }

    [Fact]
    public async Task CreatePost_UserNotAuthenticated_ReturnsUnauthorized()
    {
        // ==========================================
        // 1. ARRANGE
        // ==========================================
        // Ghi đè lại User hiện tại thành một User có ID rỗng (Guid.Empty)
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())
        }, "mock_auth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = unauthenticatedUser }
        };

        var request = new CreatePostRequest
        {
            Content = "Tôi muốn đăng bài nhưng tôi chưa đăng nhập hihi!"
        };

        // ==========================================
        // 2. ACT & ASSERT
        // ==========================================
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _controller.CreatePost(request)
        );
        Assert.Equal("User identity not found in token.", exception.Message);

        // Đảm bảo không có bài viết "rác" nào bị lọt vào Database
        var postsInDb = await _context.Posts.ToListAsync();
        Assert.Empty(postsInDb);
    }
}