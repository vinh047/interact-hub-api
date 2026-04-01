using InteractHub.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.StoryControllerTests;

public class DeleteStoryTests : StoryControllerTestBase
{
    [Fact]
    public async Task DeleteStory_Success_ReturnsOk()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        _mockStoryService.Setup(s => s.DeleteStoryAsync(storyId, _testUserId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteStory(storyId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteStory_StoryNotFound_ReturnsNotFound()
    {
        // Arrange
        var storyId = Guid.NewGuid();
        _mockStoryService.Setup(s => s.DeleteStoryAsync(storyId, _testUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteStory(storyId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}