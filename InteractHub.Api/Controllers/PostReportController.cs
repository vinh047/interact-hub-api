using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.PostReport;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/posts/{postId}/report")]
[ApiController]
[Authorize]
public class PostReportController(ApplicationDbContext context) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> ReportPost(Guid postId, [FromBody] CreateReportRequest request)
    {
        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists) return NotFound(new { message = "Post not found." });

        var report = new PostReport
        {
            PostId = postId,
            ReporterId = CurrentUserId,
            Reason = request.Reason,
            Status = ReportStatus.Pending
        };

        context.PostReports.Add(report);
        await context.SaveChangesAsync();

        return Ok(new { message = "Post reported successfully. Admins will review it." });
    }
}