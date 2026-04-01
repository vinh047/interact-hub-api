using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public class GetRepliesByCommentIdTests : CommentControllerTestBase
{
    [Fact]
    public async Task GetReplies_ParentCommentNotFound_ReturnsNotFound()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var queryParams = new CommentParams();

        _mockCommentService.Setup(s => s.GetRepliesByCommentIdAsync(commentId, queryParams))
            .ReturnsAsync((PagedList<CommentResponse>?)null);

        // Act
        var result = await _controller.GetRepliesByCommentId(commentId, queryParams);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetReplies_ParentCommentExists_ReturnsOk()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var queryParams = new CommentParams();
        
        var pagedList = new PagedList<CommentResponse>(new List<CommentResponse>(), 0, 1, 10);

        _mockCommentService.Setup(s => s.GetRepliesByCommentIdAsync(commentId, queryParams))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetRepliesByCommentId(commentId, queryParams);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}