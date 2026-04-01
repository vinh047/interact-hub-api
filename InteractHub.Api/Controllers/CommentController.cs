using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Authorize]
public class CommentController(ICommentService commentService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
    {
        var result = await commentService.CreateCommentAsync(request, CurrentUserId);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentRequest request)
    {
        var result = await commentService.UpdateCommentAsync(id, request, CurrentUserId);
        
        if (result == null) 
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Comment not found or has been deleted."));
            
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var success = await commentService.DeleteCommentAsync(id, CurrentUserId);
        
        if (!success) 
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Comment not found or already deleted."));
            
        return Ok(new { message = "Comment deleted successfully." });
    }

    [HttpGet("post/{postId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRootCommentsByPostId(Guid postId, [FromQuery] CommentParams commentParams)
    {
        var pagedComments = await commentService.GetRootCommentsByPostIdAsync(postId, commentParams);
        
        if (pagedComments == null) 
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        Response.AddPaginationHeader(pagedComments.CurrentPage, pagedComments.PageSize, pagedComments.TotalCount, pagedComments.TotalPages);
        
        return Ok(pagedComments);
    }

    [HttpGet("{commentId}/replies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRepliesByCommentId(Guid commentId, [FromQuery] CommentParams commentParams)
    {
        var pagedReplies = await commentService.GetRepliesByCommentIdAsync(commentId, commentParams);
        
        if (pagedReplies == null) 
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Parent comment not found."));

        Response.AddPaginationHeader(pagedReplies.CurrentPage, pagedReplies.PageSize, pagedReplies.TotalCount, pagedReplies.TotalPages);
        
        return Ok(pagedReplies);
    }
}