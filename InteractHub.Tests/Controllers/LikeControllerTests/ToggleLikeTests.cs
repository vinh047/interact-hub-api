using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.LikeControllerTests;

public class ToggleLikeTests : LikeControllerTestBase
{
    [Fact]
    public async Task ToggleLike_WhenReturnsTrue_ReturnsOkWithLikedMessage()
    {
        var postId = Guid.NewGuid();
        _mockLikeService.Setup(s => s.ToggleLikeAsync(postId, _testUserId)).ReturnsAsync(true);

        var result = await _controller.ToggleLike(postId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        // Cam kết với trình biên dịch là value không thể null
        Assert.NotNull(value); 
        
        // Thêm value! để chặn warning CS8602
        var isLikedProperty = value!.GetType().GetProperty("isLiked")?.GetValue(value, null);
        Assert.Equal(true, isLikedProperty);
    }

    [Fact]
    public async Task ToggleLike_WhenReturnsFalse_ReturnsOkWithUnlikedMessage()
    {
        var postId = Guid.NewGuid();
        _mockLikeService.Setup(s => s.ToggleLikeAsync(postId, _testUserId)).ReturnsAsync(false);

        var result = await _controller.ToggleLike(postId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        // Cam kết với trình biên dịch là value không thể null
        Assert.NotNull(value);
        
        // Thêm value! để chặn warning CS8602
        var isLikedProperty = value!.GetType().GetProperty("isLiked")?.GetValue(value, null);
        Assert.Equal(false, isLikedProperty);
    }
}