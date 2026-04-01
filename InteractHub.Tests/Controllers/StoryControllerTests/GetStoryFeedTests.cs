using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.StoryControllerTests;

public class GetStoryFeedTests : StoryControllerTestBase
{
    [Fact]
    public async Task GetStoryFeed_ReturnsOk_WithPagedStories()
    {
        // Arrange
        var queryParams = new StoryParams();
        var pagedList = new PagedList<StoryResponse>(new List<StoryResponse>(), 0, 1, 10);

        _mockStoryService.Setup(s => s.GetStoryFeedAsync(queryParams, _testUserId))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetStoryFeed(queryParams);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedList, okResult.Value);
        
        // Kiểm tra xem Header phân trang có được gắn vào chưa
        Assert.True(_controller.Response.Headers.ContainsKey("X-Pagination"));
    }
}