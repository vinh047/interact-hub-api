using System.Security.Claims;
using InteractHub.Api.Controllers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public abstract class CommentControllerTestBase
{
    protected readonly Mock<ICommentService> _mockCommentService;
    protected readonly CommentController _controller;
    protected readonly Guid _testUserId = Guid.NewGuid();

    protected CommentControllerTestBase()
    {
        _mockCommentService = new Mock<ICommentService>();
        _controller = new CommentController(_mockCommentService.Object);

        // Bơm User giả vào Controller
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