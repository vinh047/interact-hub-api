using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Authorize]
public class StoryController(IStoryService storyService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateStory([FromForm] CreateStoryRequest request)
    {
        // Lỗi không có file (ArgumentException) sẽ văng ra và Middleware tự lo
        var result = await storyService.CreateStoryAsync(request, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetStoryFeed([FromQuery] StoryParams storyParams)
    {
        var pagedStories = await storyService.GetStoryFeedAsync(storyParams, CurrentUserId);
        
        Response.AddPaginationHeader(pagedStories.CurrentPage, pagedStories.Limit, pagedStories.TotalCount, pagedStories.TotalPages);
        return Ok(pagedStories);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStory(Guid id)
    {
        // Lỗi phân quyền 403 đã bị Middleware tóm gọn
        var success = await storyService.DeleteStoryAsync(id, CurrentUserId);
        
        if (!success)
            return NotFound(new ErrorResponse(ErrorCode.STORY_NOT_FOUND, "Story not found or already expired."));
            
        return Ok(new { message = "Story deleted successfully." });
    }

    [HttpGet("archive")]
    public async Task<IActionResult> GetStoryArchive([FromQuery] StoryParams storyParams)
    {
        var pagedArchive = await storyService.GetStoryArchiveAsync(storyParams, CurrentUserId);
        
        Response.AddPaginationHeader(pagedArchive.CurrentPage, pagedArchive.Limit, pagedArchive.TotalCount, pagedArchive.TotalPages);
        return Ok(pagedArchive);
    }
}