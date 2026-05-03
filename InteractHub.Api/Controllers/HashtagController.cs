using InteractHub.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HashtagController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingHashtags()
    {
        var trending = await context.Hashtags
            .OrderByDescending(h => h.TrendingScore)
            .Take(10)
            .Select(h => new { h.Id, h.Name, h.TrendingScore }) // Trả về object ẩn danh cho nhanh
            .ToListAsync();

        return Ok(trending);
    }

    // [HttpGet("{name}/posts")]
    // public async Task<IActionResult> GetPostsByHashtag(string name)
    // {
    //     if (string.IsNullOrWhiteSpace(name)) return BadRequest("Hashtag name is required.");

    //     var tagLower = name.ToLower();

    //     // Tìm xem hashtag này có tồn tại không
    //     var hashtagExists = await context.Hashtags.AnyAsync(h => h.Name == tagLower);
    //     if (!hashtagExists) return Ok(new List<object>()); // Trả về mảng rỗng nếu không có

    //     // Lấy các bài viết chứa hashtag này
    //     var posts = await context.Posts
    //         .Include(p => p.User)
    //         // .Include(p => p.MediaFiles) // (Nếu có)
    //         .Where(p => !p.IsDeleted && p.Hashtags.Any(ph => ph.Name == tagLower))
    //         .OrderByDescending(p => p.CreatedAt)
    //         // NHỚ MAP SANG DTO ĐỂ KHÔNG BỊ LỖI VÒNG LẶP JSON
    //         .Select(p => new 
    //         {
    //             p.Id,
    //             p.Content,
    //             AuthorName = p.User.FullName,
    //             AuthorAvatar = p.User.AvatarUrl,
    //             // ... các trường khác
    //         })
    //         .ToListAsync();

    //     return Ok(posts);
    // }
}