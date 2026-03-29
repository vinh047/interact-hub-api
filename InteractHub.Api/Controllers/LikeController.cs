using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bắt buộc phải đăng nhập mới được thả tim
public class LikeController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost("post/{postId}")]
    public async Task<IActionResult> ToggleLike(Guid postId)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Kiểm tra bài viết có tồn tại (và chưa bị xóa) không
        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
        {
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found or has been deleted."));
        }

        // Tìm xem User này đã Like bài viết này chưa
        var existingLike = await context.Likes
            .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == userId);

        if (existingLike != null)
        {
            // NẾU ĐÃ LIKE -> Thực hiện UNLIKE (Xóa vật lý dòng Like này)
            context.Likes.Remove(existingLike);
            await context.SaveChangesAsync();
            
            return Ok(new { message = "Post unliked successfully.", isLiked = false });
        }
        else
        {
            // NẾU CHƯA LIKE -> Thực hiện LIKE (Thêm mới)
            var newLike = new Like
            {
                PostId = postId,
                UserId = userId
            };
            
            context.Likes.Add(newLike);
            await context.SaveChangesAsync();
            
            return Ok(new { message = "Post liked successfully.", isLiked = true });
        }
    }
}