using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public class GetRootCommentsByPostIdTests : CommentControllerTestBase
{
    [Fact]
    public async Task GetRootComments_PostNotFound_ReturnsNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var queryParams = new CommentParams();

        _mockCommentService.Setup(s => s.GetRootCommentsByPostIdAsync(postId, queryParams))
            .ReturnsAsync((PagedList<CommentResponse>?)null); // Kỹ sư báo Post bị xóa rồi

        // Act
        var result = await _controller.GetRootCommentsByPostId(postId, queryParams);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetRootComments_PostExists_ReturnsOk()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var queryParams = new CommentParams();
        
        // Tạo một PagedList rỗng để giả lập
        var pagedList = new PagedList<CommentResponse>(new List<CommentResponse>(), 0, 1, 10);

        _mockCommentService.Setup(s => s.GetRootCommentsByPostIdAsync(postId, queryParams))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetRootCommentsByPostId(postId, queryParams);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}