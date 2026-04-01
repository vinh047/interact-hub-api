using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Services.FriendshipServiceTests;

public abstract class FriendshipServiceTestBase : IDisposable
{
    protected readonly ApplicationDbContext _context;
    protected readonly FriendshipService _friendshipService;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected FriendshipServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _friendshipService = new FriendshipService(_context);

        // Nạp sẵn bản thân mình vào DB để tránh lỗi khóa ngoại
        _context.Users.Add(new ApplicationUser 
        { 
            Id = _testUserId, FullName = "Vinh Test", AvatarUrl = "vinh.jpg" 
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}