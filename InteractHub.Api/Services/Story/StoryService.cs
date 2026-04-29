using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Services;

public class StoryService(ApplicationDbContext context, IFileService fileService) : IStoryService
{
    public async Task<StoryResponse> CreateStoryAsync(CreateStoryRequest request, Guid currentUserId)
    {
        if (request.MediaFile == null || request.MediaFile.Length == 0)
        {
            // Ném lỗi để Middleware biến nó thành 400 BadRequest
            throw new ArgumentException("Please select a file to upload.");
        }

        // Gọi File Service lưu ảnh
        var mediaUrl = await fileService.UploadFileAsync(request.MediaFile, "stories");

        var newStory = new Story
        {
            MediaUrl = mediaUrl,
            UserId = currentUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        context.Stories.Add(newStory);
        await context.SaveChangesAsync();

        return await context.Stories
            .Include(s => s.User)
            .Where(s => s.Id == newStory.Id)
            .MapToStoryResponse()
            .FirstAsync();
    }

    public async Task<PagedList<StoryResponse>> GetStoryFeedAsync(StoryParams storyParams, Guid currentUserId)
    {
        // Lấy danh sách bạn bè đã đồng ý kết bạn
        var friendIds = await context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterId == currentUserId || f.AddresseeId == currentUserId))
            .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        var idsToQuery = friendIds;
        idsToQuery.Add(currentUserId); // Gộp thêm chính mình vào

        var query = context.Stories
             .Include(s => s.User)
             .Where(s => idsToQuery.Contains(s.UserId))
             .OrderByDescending(s => s.CreatedAt)
             .MapToStoryResponse();

        return await PagedList<StoryResponse>.CreateAsync(query, storyParams.Page, storyParams.Limit);
    }

    public async Task<bool> DeleteStoryAsync(Guid id, Guid currentUserId)
    {
        var story = await context.Stories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (story == null) return false;

        if (story.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to delete this story.");

        context.Stories.Remove(story);
        await context.SaveChangesAsync();
        
        return true;
    }

    public async Task<PagedList<StoryResponse>> GetStoryArchiveAsync(StoryParams storyParams, Guid currentUserId)
    {
        var query = context.Stories
            .IgnoreQueryFilters() // Bỏ lọc để lấy cả Story hết hạn
            .Include(s => s.User)
            .Where(s => s.UserId == currentUserId)
            .OrderByDescending(s => s.CreatedAt)
            .MapToStoryResponse();

        return await PagedList<StoryResponse>.CreateAsync(query, storyParams.Page, storyParams.Limit);
    }
}