using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InteractHub.Tests.Services.StoryServiceTests;

public abstract class StoryServiceTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly Mock<IFileService> _mockFileService;
    protected readonly StoryService _storyService;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected StoryServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        
        // Khởi tạo Mock cho File Service
        _mockFileService = new Mock<IFileService>();
        
        _storyService = new StoryService(_context, _mockFileService.Object);

        // Nạp sẵn User giả
        _context.Users.Add(new ApplicationUser { Id = _testUserId, FullName = "Vinh Test", Email = "vinh@test.com" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}