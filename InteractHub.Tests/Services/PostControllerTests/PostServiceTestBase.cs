using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InteractHub.Tests.Services.PostServiceTests;

public abstract class PostServiceTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly Mock<IFileService> _mockFileService;
    protected readonly PostService _postService;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected PostServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _mockFileService = new Mock<IFileService>();
        _postService = new PostService(_context, _mockFileService.Object);

        // Nạp sẵn User
        _context.Users.Add(new ApplicationUser { Id = _testUserId, FullName = "Test" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}