using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Services;

public class LikeService(ApplicationDbContext context) : ILikeService
{
    public async Task<bool> ToggleLikeAsync(Guid postId, Guid currentUserId)
    {
        // 1. Kiểm tra bài viết có tồn tại không
        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
            throw new KeyNotFoundException("Post not found or has been deleted.");

        // 2. Tìm xem User này đã Like bài viết này chưa
        var existingLike = await context.Likes
            .FirstOrDefaultAsync(like => like.PostId == postId && like.UserId == currentUserId);

        if (existingLike != null)
        {
            // Đã Like -> Gỡ bỏ (Unlike)
            context.Likes.Remove(existingLike);
            await context.SaveChangesAsync();
            return false; // isLiked = false
        }
        
        // Chưa Like -> Thêm vào (Like)
        context.Likes.Add(new Like { PostId = postId, UserId = currentUserId });
        await context.SaveChangesAsync();
        return true; // isLiked = true
    }
}