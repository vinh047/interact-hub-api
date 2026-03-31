using InteractHub.Api.Entities;
using InteractHub.Api.Enums;

namespace InteractHub.Tests.Services.PostServiceTests;

public class GetPostByIdAsyncTests : PostServiceTestBase
{
    [Fact]
    public async Task GetPostByIdAsync_PrivatePost_ReturnsNull_ForOtherUser()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        // Tạo User lạ trong DB Ảo
        _context.Users.Add(new ApplicationUser { Id = anotherUserId, FullName = "Người Lạ", Email = "a@a.com" });
        _context.Posts.Add(new Post { Id = postId, Content = "Bí mật", UserId = anotherUserId, Visibility = PostVisibility.Private });
        await _context.SaveChangesAsync();

        // Act: Mình (testUserId) đi xem bài Private của người lạ
        var result = await _postService.GetPostByIdAsync(postId, _testUserId);

        // Assert
        Assert.Null(result); // Service chặn lại và trả về null (Controller sẽ báo 404)
    }
}