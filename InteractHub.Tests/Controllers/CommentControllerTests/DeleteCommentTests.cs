using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InteractHub.Tests.Controllers.CommentControllerTests;

public class DeleteCommentTests : CommentControllerTestBase
{
    [Fact]
    public async Task DeleteComment_Success_ReturnsOk()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        
        _mockCommentService.Setup(s => s.DeleteCommentAsync(commentId, _testUserId))
            .ReturnsAsync(true); // Kỹ Sư báo xóa thành công

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteComment_CommentNotFound_ReturnsNotFound()
    {
        // Arrange
        var commentId = Guid.NewGuid();

        _mockCommentService.Setup(s => s.DeleteCommentAsync(commentId, _testUserId))
            .ReturnsAsync(false); // Kỹ Sư báo không tìm thấy

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}