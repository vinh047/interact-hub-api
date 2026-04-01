using System.Security.Claims;
using InteractHub.Api.Controllers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Controllers.LikeControllerTests;

public abstract class LikeControllerTestBase
{
    protected readonly Mock<ILikeService> _mockLikeService;
    protected readonly LikeController _controller;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected LikeControllerTestBase()
    {
        _mockLikeService = new Mock<ILikeService>();
        _controller = new LikeController(_mockLikeService.Object);

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