using System.Security.Claims;
using InteractHub.Api.Controllers;
using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InteractHub.Tests.Controllers.PostControllerTests;

// Abstract class để không ai được quyền chạy test trực tiếp trên file này
public abstract class PostControllerTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly Mock<IFileService> _mockFileService;
    protected readonly PostController _controller;
    protected readonly Guid _testUserId = Guid.NewGuid();

    public PostControllerTestBase()
    {
        // 1. Setup In-Memory Database (DB Ảo)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // DB name random để không đụng hàng
            .Options;
        _context = new ApplicationDbContext(options);

        // Tạo sẵn 1 User ảo ném vào DB để lát nữa Controller query Include(p => p.User) không bị lỗi
        _context.Users.Add(new ApplicationUser
        {
            Id = _testUserId,
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = "hashed123"
        });
        _context.SaveChanges();

        // 2. Dùng Moq làm giả thợ xây IFileService (Test thì không được lưu file ra ổ cứng thật)
        _mockFileService = new Mock<IFileService>();

        // 3. Khởi tạo Controller với các đồ giả (Mock)
        _controller = new PostController(_context, _mockFileService.Object);

        // 4. Giả lập người dùng đã Đăng nhập (Vì API có gắn [Authorize])
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) // Khớp với hàm GetUserId() của bạn
        }, "mock_auth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // Best Practice: Chạy test xong thì tự động dọn rác DB ảo để không ảnh hưởng các test sau
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}