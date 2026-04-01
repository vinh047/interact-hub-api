using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.FriendshipControllerTests;

public class SendFriendRequestTests : FriendshipControllerTestBase
{
    [Fact]
    public async Task SendFriendRequest_Success_ReturnsOk()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        
        // Mock service chạy thành công (không ném lỗi)
        _mockFriendshipService.Setup(s => s.SendFriendRequestAsync(targetUserId, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendFriendRequest(targetUserId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}