using InteractHub.Api.DTOs.Requests.Story;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InteractHub.Tests.Services.StoryServiceTests;

public class CreateStoryAsyncTests : StoryServiceTestBase
{
    [Fact]
    public async Task CreateStoryAsync_EmptyFile_ThrowsArgumentException()
    {
        // Arrange: File bị null
        var request = new CreateStoryRequest { MediaFile = null! };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _storyService.CreateStoryAsync(request, _testUserId)
        );
        Assert.Equal("Please select a file to upload.", exception.Message);
    }

    [Fact]
    public async Task CreateStoryAsync_ValidFile_UploadsAndReturnsStory()
    {
        // Arrange: Tạo 1 file ảo
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024); // File nặng 1KB (không bị rỗng)
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        var request = new CreateStoryRequest { MediaFile = mockFile.Object };

        // Dạy Kỹ sư: Khi có ai nhờ upload file, hãy nhả ra cái link ảo này
        _mockFileService
            .Setup(s => s.UploadFileAsync(mockFile.Object, "stories"))
            .ReturnsAsync("https://interacthub.local/stories/test.jpg");

        // Act
        var result = await _storyService.CreateStoryAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://interacthub.local/stories/test.jpg", result.MediaUrl);
        Assert.Equal(_testUserId, result.AuthorId);

        // Check Db
        var dbStory = await _context.Stories.FirstOrDefaultAsync();
        Assert.NotNull(dbStory);
        Assert.Equal("https://interacthub.local/stories/test.jpg", dbStory.MediaUrl);
    }
}