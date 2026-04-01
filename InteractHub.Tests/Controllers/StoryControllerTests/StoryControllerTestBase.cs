using System.Security.Claims;
using InteractHub.Api.Controllers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Controllers.StoryControllerTests;

public abstract class StoryControllerTestBase
{
    protected readonly Mock<IStoryService> _mockStoryService;
    protected readonly StoryController _controller;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected StoryControllerTestBase()
    {
        _mockStoryService = new Mock<IStoryService>();
        _controller = new StoryController(_mockStoryService.Object);

        // Bơm User giả và HttpContext giả (để hàm Response.AddPaginationHeader không bị lỗi)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock_auth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}