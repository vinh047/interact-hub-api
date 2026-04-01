using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.DTOs.Responses.Friendship;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.FriendshipControllerTests;

public class GetFriendsTests : FriendshipControllerTestBase
{
    [Fact]
    public async Task GetFriends_ReturnsOk_WithPagedFriends()
    {
        // Arrange
        var queryParams = new FriendshipParams();
        var pagedList = new PagedList<FriendUserResponse>(new List<FriendUserResponse>(), 0, 1, 10);

        _mockFriendshipService.Setup(s => s.GetFriendsAsync(queryParams, _testUserId))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetFriends(queryParams);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedList, okResult.Value);
        Assert.True(_controller.Response.Headers.ContainsKey("X-Pagination"));
    }
}