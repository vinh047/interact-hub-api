using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public class UpdateCommentTests : CommentControllerTestBase
{
    [Fact]
    public async Task UpdateComment_CommentExists_ReturnsOkResult()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var request = new UpdateCommentRequest { Content = "Đã sửa nội dung" };
        var mockResponse = new CommentResponse { Id = commentId, Content = "Đã sửa nội dung", AuthorName = "Test User" };

        _mockCommentService.Setup(s => s.UpdateCommentAsync(commentId, request, _testUserId))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.UpdateComment(commentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(mockResponse, okResult.Value);
    }

    [Fact]
    public async Task UpdateComment_CommentNotFound_ReturnsNotFound()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var request = new UpdateCommentRequest { Content = "Sửa comment ma" };

        // Giả lập Kỹ Sư tìm không ra, trả về null
        _mockCommentService.Setup(s => s.UpdateCommentAsync(commentId, request, _testUserId))
            .ReturnsAsync((CommentResponse?)null);

        // Act
        var result = await _controller.UpdateComment(commentId, request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}