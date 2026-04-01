using System.Security.Claims;
using InteractHub.Api.Controllers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Controllers.FriendshipControllerTests;

public abstract class FriendshipControllerTestBase
{
    protected readonly Mock<IFriendshipService> _mockFriendshipService;
    protected readonly FriendshipController _controller;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected FriendshipControllerTestBase()
    {
        _mockFriendshipService = new Mock<IFriendshipService>();
        _controller = new FriendshipController(_mockFriendshipService.Object);

        // Bơm User giả và HttpContext giả để các hàm lấy ID và gắn Header phân trang hoạt động mượt mà
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