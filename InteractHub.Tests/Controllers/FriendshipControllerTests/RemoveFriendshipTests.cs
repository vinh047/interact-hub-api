using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.FriendshipControllerTests;

public class RemoveFriendshipTests : FriendshipControllerTestBase
{
    [Fact]
    public async Task RemoveFriendship_Success_ReturnsOk()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _mockFriendshipService.Setup(s => s.RemoveFriendshipAsync(otherUserId, _testUserId)).ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveFriendship(otherUserId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveFriendship_NotFound_ReturnsNotFound()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _mockFriendshipService.Setup(s => s.RemoveFriendshipAsync(otherUserId, _testUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveFriendship(otherUserId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}