using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.StoryControllerTests;

public class GetStoryArchiveTests : StoryControllerTestBase
{
    [Fact]
    public async Task GetStoryArchive_ReturnsOk_WithPagedStories()
    {
        // Arrange
        var queryParams = new StoryParams();
        var pagedList = new PagedList<StoryResponse>(new List<StoryResponse>(), 0, 1, 10);

        _mockStoryService.Setup(s => s.GetStoryArchiveAsync(queryParams, _testUserId))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetStoryArchive(queryParams);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedList, okResult.Value);
    }
}