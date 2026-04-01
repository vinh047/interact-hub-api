using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Authorize]
public class LikeController(ILikeService likeService) : BaseController
{
    [HttpPost("post/{postId}")]
    public async Task<IActionResult> ToggleLike(Guid postId)
    {
        var isLiked = await likeService.ToggleLikeAsync(postId, CurrentUserId);
        
        return Ok(new 
        { 
            message = isLiked ? "Post liked successfully." : "Post unliked successfully.", 
            isLiked = isLiked 
        });
    }
}