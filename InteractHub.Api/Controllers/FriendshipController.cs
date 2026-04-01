using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Authorize]
public class FriendshipController(IFriendshipService friendshipService) : BaseController
{
    [HttpPost("request/{targetUserId}")]
    public async Task<IActionResult> SendFriendRequest(Guid targetUserId)
    {
        // Các lỗi 400 (tự kết bạn), 404 (user không tồn tại), 409 (đã gửi rồi/đã là bạn) 
        // sẽ được Middleware tự động bắt và trả về từ Service!
        await friendshipService.SendFriendRequestAsync(targetUserId, CurrentUserId);
        
        return Ok(new { message = "Friend request sent successfully." });
    }

    [HttpPut("accept/{requesterId}")]
    public async Task<IActionResult> AcceptFriendRequest(Guid requesterId)
    {
        await friendshipService.AcceptFriendRequestAsync(requesterId, CurrentUserId);
        
        return Ok(new { message = "Friend request accepted successfully." });
    }

    [HttpDelete("{otherUserId}")]
    public async Task<IActionResult> RemoveFriendship(Guid otherUserId)
    {
        var success = await friendshipService.RemoveFriendshipAsync(otherUserId, CurrentUserId);
        
        if (!success)
        {
            return NotFound(new ErrorResponse(ErrorCode.FRIENDSHIP_NOT_FOUND, "No existing relationship found with this user."));
        }

        return Ok(new { message = "Friendship or request removed successfully." });
    }

    [HttpGet("friends")]
    public async Task<IActionResult> GetFriends([FromQuery] FriendshipParams friendshipParams)
    {
        var pagedFriends = await friendshipService.GetFriendsAsync(friendshipParams, CurrentUserId);
        
        Response.AddPaginationHeader(pagedFriends.CurrentPage, pagedFriends.PageSize, pagedFriends.TotalCount, pagedFriends.TotalPages);
        
        return Ok(pagedFriends);
    }

    [HttpGet("requests/received")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] FriendshipParams friendshipParams)
    {
        var pagedRequests = await friendshipService.GetPendingRequestsAsync(friendshipParams, CurrentUserId);
        
        Response.AddPaginationHeader(pagedRequests.CurrentPage, pagedRequests.PageSize, pagedRequests.TotalCount, pagedRequests.TotalPages);
        
        return Ok(pagedRequests);
    }
}