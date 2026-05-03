using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Helpers;
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
    public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        // 1. Join với bảng User (thông qua IssuerId) để lấy tên và avatar
        var query = context.Notifications
            .Include(n => n.Issuer) // Đảm bảo Entity Notification của bạn có navigation property `public User? Issuer { get; set; }`
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type,
                Content = n.Content,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                IssuerId = n.IssuerId,
                IssuerName = n.Issuer != null ? n.Issuer.FullName : "Người dùng",
                IssuerAvatar = n.Issuer != null ? n.Issuer.AvatarUrl : null
            });

        // 2. Sử dụng PagedList của dự án để phân trang
        var pagedNotifications = await PagedList<NotificationResponse>.CreateAsync(query, page, limit);

        // 3. Đính kèm Header phân trang cho FE
        Response.AddPaginationHeader(pagedNotifications.CurrentPage, pagedNotifications.Limit, pagedNotifications.TotalCount, pagedNotifications.TotalPages);

        return Ok(pagedNotifications);
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        // Lấy các thông báo chưa đọc của user hiện tại
        var unreadNotifications = await context.Notifications
            .Where(n => n.UserId == CurrentUserId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            foreach (var notif in unreadNotifications)
            {
                notif.IsRead = true;
            }
            await context.SaveChangesAsync();
        }

        return Ok(new { message = "Marked all as read." });
    }
}