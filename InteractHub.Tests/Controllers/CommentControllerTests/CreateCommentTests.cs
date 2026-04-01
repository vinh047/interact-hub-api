using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public class CreateCommentTests : CommentControllerTestBase
{
    [Fact]
    public async Task CreateComment_ReturnsOkResult_WithCreatedComment()
    {
        // Arrange
        var request = new CreateCommentRequest { PostId = Guid.NewGuid(), Content = "Bài viết hay quá!" };
        var mockResponse = new CommentResponse { Id = Guid.NewGuid(), Content = "Bài viết hay quá!", AuthorName = "Test User" };

        // Giả lập Kỹ Sư làm việc thành công và trả về dữ liệu
        _mockCommentService.Setup(s => s.CreateCommentAsync(request, _testUserId))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _controller.CreateComment(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(mockResponse, okResult.Value);
    }
}