using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses.Story;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.StoryControllerTests;

public class CreateStoryTests : StoryControllerTestBase
{
    [Fact]
    public async Task CreateStory_ValidRequest_ReturnsOk()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        var request = new CreateStoryRequest { MediaFile = mockFile.Object };
        
        var mockResponse = new StoryResponse { Id = Guid.NewGuid(), MediaUrl = "test.jpg", AuthorId = _testUserId, AuthorName = "Test User" };

        // Giả lập Kỹ sư làm việc trót lọt
        _mockStoryService.Setup(s => s.CreateStoryAsync(request, _testUserId))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.CreateStory(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(mockResponse, okResult.Value);
    }
}