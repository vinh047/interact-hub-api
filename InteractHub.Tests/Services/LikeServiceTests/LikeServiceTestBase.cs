using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Services.LikeServiceTests;

public abstract class LikeServiceTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly LikeService _likeService;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected LikeServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _likeService = new LikeService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}