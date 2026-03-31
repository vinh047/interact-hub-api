using Xunit;
using InteractHub.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Http;

namespace InteractHub.Tests.Controllers.PostControllerTests;

public class DeletePostTests : PostControllerTestBase // <-- Kế thừa ở đây
{
    #region DeletePost API Tests

    [Fact]
    public async Task DeletePost_AsOwner_ReturnsOkResult_AndSoftDeletes()
    {
        // ==========================================
        // 1. ARRANGE: Tạo 1 bài viết do chính _testUserId làm chủ
        // ==========================================
        var postId = Guid.NewGuid();
        var post = new Post
        {
            Id = postId,
            Content = "Bài viết này chuẩn bị bốc hơi",
            UserId = _testUserId, // Quan trọng: Mình phải là tác giả
            Visibility = PostVisibility.Public
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // ==========================================
        // 2. ACT: Gọi API xóa bài viết đó
        // ==========================================
        var result = await _controller.DeletePost(postId);

        // ==========================================
        // 3. ASSERT: Xác nhận kết quả
        // ==========================================
        
        // A. Trả về 200 OK
        var okResult = Assert.IsType<OkObjectResult>(result);

        // B. Kiểm tra cơ chế Soft Delete trong DB ảo
        // (Dùng IgnoreQueryFilters để lôi cả những bài đã bị xóa mềm lên kiểm tra)
        var deletedPost = await _context.Posts
            .IgnoreQueryFilters() 
            .FirstOrDefaultAsync(p => p.Id == postId);

        Assert.NotNull(deletedPost); // Bài viết vẫn phải nằm trong DB, không được bay màu
        Assert.True(deletedPost.IsDeleted); // Cờ IsDeleted phải được bật thành true
        Assert.NotNull(deletedPost.DeletedAt); // Thời gian xóa phải được ghi nhận
    }

    #endregion

    // ==========================================
    // NEGATIVE PATHS (Đường rủi ro)
    // ==========================================

    [Fact]
    public async Task DeletePost_PostDoesNotExist_ReturnsNotFound()
    {
        // 1. ARRANGE: Bịa ra một cái ID ngẫu nhiên không có trong DB ảo
        var fakePostId = Guid.NewGuid();

        // 2. ACT: Cố tình gọi hàm xóa với cái ID "ma" đó
        var result = await _controller.DeletePost(fakePostId);

        // 3. ASSERT: Hệ thống phải chửi 404 Not Found
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        
        // (Tùy chọn) Có thể check luôn câu thông báo lỗi xem có chuẩn không
        // var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        // Assert.Equal(ErrorCode.POST_NOT_FOUND, errorResponse.Code);
    }

    [Fact]
    public async Task DeletePost_UserIsNotAuthor_ReturnsForbidden()
    {
        // 1. ARRANGE: Tạo một bài viết nhưng tác giả là "một thằng ất ơ nào đó"
        var postId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid(); // ID lạ, không phải _testUserId (mình)

        var existingPost = new Post
        {
            Id = postId,
            Content = "Bài viết của người ta, cấm đụng!",
            UserId = anotherUserId, 
            Visibility = PostVisibility.Public
        };
        
        _context.Posts.Add(existingPost);
        await _context.SaveChangesAsync();

        // 2. ACT: Mình (đang đóng vai _testUserId) cố tình gọi API xóa bài của nó
        var result = await _controller.DeletePost(postId);

        // 3. ASSERT: Kiểm tra xem hệ thống có "đá" mình ra bằng lỗi 403 không
        
        // Phải trả về kiểu ObjectResult (vì trong Controller bạn dùng StatusCode(StatusCodes.Status403Forbidden, ...))
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        
        // Mã lỗi phải chuẩn xác là 403
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);

        // QUAN TRỌNG NHẤT: Kiểm tra Database xem bài viết vẫn phải còn nguyên, KHÔNG ĐƯỢC BỊ XÓA (IsDeleted vẫn là false)
        var postInDb = await _context.Posts.FindAsync(postId);
        Assert.False(postInDb!.IsDeleted); 
    }
}