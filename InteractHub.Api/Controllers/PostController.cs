using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Authorize]
public class PostController(IPostService postService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
    {
        var result = await postService.CreatePostAsync(request, CurrentUserId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetNewsFeed([FromQuery] PostQueryParameters queryParams)
    {
        var pagedPosts = await postService.GetNewsFeedAsync(queryParams, CurrentUserId);
        Response.AddPaginationHeader(pagedPosts.CurrentPage, pagedPosts.Limit, pagedPosts.TotalCount, pagedPosts.TotalPages);
        return Ok(pagedPosts);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var success = await postService.DeletePostAsync(id, CurrentUserId);
        
        if (!success) return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));
        return Ok(new { message = "Post deleted successfully." });
    }

    [HttpPatch("{id}/visibility")]
    public async Task<IActionResult> ChangeVisibility(Guid id, [FromBody] ChangeVisibilityRequest request)
    {
        // GỌN GÀNG!
        var result = await postService.ChangeVisibilityAsync(id, request.Visibility, CurrentUserId);
        if (result == null) return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(Guid userId, [FromQuery] PostQueryParameters queryParams)
    {
        var pagedPosts = await postService.GetUserPostsAsync(userId, queryParams, CurrentUserId);
        Response.AddPaginationHeader(pagedPosts.CurrentPage, pagedPosts.Limit, pagedPosts.TotalCount, pagedPosts.TotalPages);
        return Ok(pagedPosts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var post = await postService.GetPostByIdAsync(id, CurrentUserId);
        if (post == null) return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));
        return Ok(post);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostRequest request)
    {
        // CỰC KỲ MỎNG NHẸ!
        var result = await postService.UpdatePostAsync(id, request, CurrentUserId);
        if (result == null) return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));
        return Ok(result);
    }
}