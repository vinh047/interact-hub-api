using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.FriendshipControllerTests;

public class AcceptFriendRequestTests : FriendshipControllerTestBase
{
    [Fact]
    public async Task AcceptFriendRequest_Success_ReturnsOk()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        
        _mockFriendshipService.Setup(s => s.AcceptFriendRequestAsync(requesterId, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AcceptFriendRequest(requesterId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}