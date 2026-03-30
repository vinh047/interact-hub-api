using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bắt buộc đăng nhập
public class StoryController(ApplicationDbContext context, IFileService fileService) : ControllerBase
{
    // API: Đăng Story
    [HttpPost]
    public async Task<IActionResult> CreateStory([FromForm] CreateStoryRequest request)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Validate chống upload file rỗng
        if (request.MediaFile == null || request.MediaFile.Length == 0)
        {
            return BadRequest(new ErrorResponse(ErrorCode.BAD_REQUEST, "Please select a file to upload."));
        }

        // MAGIC HAPPENS HERE: Lưu file vào thư mục "wwwroot/stories" và lấy link URL
        var mediaUrl = await fileService.UploadFileAsync(request.MediaFile, "stories");

        // Tạo Entity Story mới với cái link vừa nhận được
        var newStory = new Story
        {
            MediaUrl = mediaUrl, // Gán link xịn vào
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        context.Stories.Add(newStory);
        await context.SaveChangesAsync();

        return Ok(new { message = "Story posted successfully.", storyId = newStory.Id });
    }

    // API: Lấy Story Feed
    [HttpGet("feed")]
    public async Task<IActionResult> GetStoryFeed()
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Lấy danh sách ID của bạn bè (Accepted)
        var friendIds = await context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterId == currentUserId || f.AddresseeId == currentUserId))
            .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        // Gom chung ID bản thân vào danh sách để lấy cả Story của mình
        var idsToQuery = friendIds;
        idsToQuery.Add(currentUserId);

        // Truy vấn Story Feed
        var storyFeed = await context.Stories
            .Include(s => s.User) // Lấy thông tin tác giả
            .Where(s => idsToQuery.Contains(s.UserId)) // Chỉ lấy của mình và bạn bè
            .OrderByDescending(s => s.CreatedAt) // Mới nhất xếp trước
            .Select(s => new StoryResponse
            {
                Id = s.Id,
                MediaUrl = s.MediaUrl,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                AuthorId = s.UserId,
                AuthorName = s.User!.FullName,
                AuthorAvatarUrl = s.User!.AvatarUrl
            })
            .ToListAsync();

        return Ok(storyFeed);
    }

    // API: Xóa Story (Chủ động xóa trước 24h)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStory(Guid id)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Tìm Story (cũng ăn theo Global Query Filter nên nếu Story đã quá 24h, nó sẽ trả về null)
        var story = await context.Stories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (story == null)
        {
            return NotFound(new ErrorResponse(ErrorCode.STORY_NOT_FOUND, "Story not found or already expired."));
        }

        // Kiểm tra phân quyền: Chỉ chủ nhân mới được xóa
        if (story.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to delete this story."));
        }

        context.Stories.Remove(story);
        await context.SaveChangesAsync();

        return Ok(new { message = "Story deleted successfully." });
    }

    // API: Xem Kho lưu trữ tin (Story Archive)
    [HttpGet("archive")]
    public async Task<IActionResult> GetStoryArchive()
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Lấy TOÀN BỘ Story của CHÍNH MÌNH (Bao gồm cả những cái đã quá 24h)
        var myArchive = await context.Stories
            .IgnoreQueryFilters() // Vô hiệu hóa bẫy ExpiresAt
            .Include(s => s.User)
            .Where(s => s.UserId == currentUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StoryResponse
            {
                Id = s.Id,
                MediaUrl = s.MediaUrl,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                AuthorId = s.UserId,
                AuthorName = s.User!.FullName,
                AuthorAvatarUrl = s.User!.AvatarUrl
            })
            .ToListAsync();

        return Ok(myArchive);
    }
}