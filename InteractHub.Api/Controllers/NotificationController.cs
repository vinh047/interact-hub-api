using InteractHub.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController(ApplicationDbContext context) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var notifications = await context.Notifications
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new 
            { 
                n.Id, n.Type, n.Content, n.ReferenceId, n.IsRead, n.CreatedAt 
            })
            .ToListAsync();

        return Ok(notifications);
    }
}