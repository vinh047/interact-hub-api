using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Services.CommentServiceTests;

public abstract class CommentServiceTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly CommentService _commentService;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected CommentServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _commentService = new CommentService(_context);

        // Nạp sẵn 1 User giả để hàm MapToCommentResponse không bị lỗi khi Join bảng
        _context.Users.Add(new ApplicationUser { Id = _testUserId, FullName = "Vinh Test", Email = "vinh@test.com" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}