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
}