using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Friendship;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.DTOs.Responses.Friendship;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FriendshipController(ApplicationDbContext context) : ControllerBase
{
    // API: Gửi lời mời kết bạn
    [HttpPost("request/{targetUserId}")]
    public async Task<IActionResult> SendFriendRequest(Guid targetUserId)
    {
        // Lấy ID của người đang đăng nhập (Người gửi)
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Không được tự kết bạn với chính mình
        if (currentUserId == targetUserId)
        {
            return BadRequest(new ErrorResponse(ErrorCode.BAD_REQUEST, "You cannot send a friend request to yourself."));
        }

        // Người nhận phải tồn tại
        var targetUserExists = await context.Users.AnyAsync(u => u.Id == targetUserId);
        if (!targetUserExists)
        {
            return NotFound(new ErrorResponse(ErrorCode.USER_NOT_FOUND, "The user you are trying to add does not exist."));
        }

        // Kiểm tra xem đã có mối quan hệ nào giữa 2 người chưa (Bất kể ai là người gửi)
        var existingFriendship = await context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == targetUserId) ||
                (f.RequesterId == targetUserId && f.AddresseeId == currentUserId)
            );

        if (existingFriendship != null)
        {
            // Phân tích kỹ hơn để trả về câu thông báo cho chuẩn
            if (existingFriendship.Status == FriendshipStatus.Accepted)
                return Conflict(new ErrorResponse(ErrorCode.CONFLICT, "You are already friends with this user."));

            if (existingFriendship.Status == FriendshipStatus.Pending)
            {
                if (existingFriendship.RequesterId == currentUserId)
                    return Conflict(new ErrorResponse(ErrorCode.CONFLICT, "You have already sent a friend request to this user."));
                else
                    return Conflict(new ErrorResponse(ErrorCode.CONFLICT, "This user has already sent you a friend request. Please accept it."));
            }

            if (existingFriendship.Status == FriendshipStatus.Blocked)
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You cannot interact with this user."));
        }

        // Tạo lời mời mới
        var newFriendship = new Friendship
        {
            RequesterId = currentUserId,
            AddresseeId = targetUserId,
            Status = FriendshipStatus.Pending
        };

        context.Friendships.Add(newFriendship);
        await context.SaveChangesAsync();

        return Ok(new { message = "Friend request sent successfully." });
    }

    // API: Chấp nhận lời mời kết bạn
    [HttpPut("accept/{requesterId}")]
    public async Task<IActionResult> AcceptFriendRequest(Guid requesterId)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Tìm đúng cái lời mời mà người kia gửi cho mình
        var friendship = await context.Friendships
            .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == currentUserId);

        // Không tìm thấy
        if (friendship == null)
        {
            return NotFound(new ErrorResponse(ErrorCode.FRIENDSHIP_NOT_FOUND, "Friend request not found."));
        }

        // Trạng thái không phải là Pending (Có thể họ đã là bạn bè rồi)
        if (friendship.Status != FriendshipStatus.Pending)
        {
            return BadRequest(new ErrorResponse(ErrorCode.BAD_REQUEST, "This friend request has already been processed."));
        }

        // 4. Chuyển trạng thái thành Accepted
        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new { message = "Friend request accepted successfully." });
    }

    // API: Hủy lời mời / Từ chối lời mời / Hủy kết bạn (Xóa mối quan hệ)
    [HttpDelete("{otherUserId}")]
    public async Task<IActionResult> RemoveFriendship(Guid otherUserId)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Tìm mối quan hệ giữa 2 người (Bất kể ai gửi ai nhận, chiều nào cũng quét)
        var friendship = await context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == otherUserId) ||
                (f.RequesterId == otherUserId && f.AddresseeId == currentUserId)
            );

        // Không có quan hệ gì để mà xóa
        if (friendship == null)
        {
            return NotFound(new ErrorResponse(ErrorCode.FRIENDSHIP_NOT_FOUND, "No existing relationship found with this user."));
        }

        // Thực hiện Hard Delete (Xóa hẳn khỏi DB)
        context.Friendships.Remove(friendship);
        await context.SaveChangesAsync();

        return Ok(new { message = "Friendship or request removed successfully." });
    }

    // API: Lấy danh sách bạn bè
    [HttpGet("friends")]
    public async Task<IActionResult> GetFriends([FromQuery] FriendshipParams friendshipParams)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Lấy danh sách bạn bè (Status = Accepted) và User hiện tại có thể là Người gửi HOẶC Người nhận
        var query = context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterId == currentUserId || f.AddresseeId == currentUserId))
            .Select(f => new FriendUserResponse
            {
                UserId = f.RequesterId == currentUserId ? f.Addressee!.Id : f.Requester!.Id,
                FullName = f.RequesterId == currentUserId ? f.Addressee!.FullName : f.Requester!.FullName,
                AvatarUrl = f.RequesterId == currentUserId ? f.Addressee!.AvatarUrl : f.Requester!.AvatarUrl,
                Bio = f.RequesterId == currentUserId ? f.Addressee!.Bio : f.Requester!.Bio,

                Status = f.Status,
                CreatedAt = f.UpdatedAt ?? f.CreatedAt
            })
            .OrderByDescending(f => f.CreatedAt);

        var pagedFriends = await PagedList<FriendUserResponse>.CreateAsync(query, friendshipParams.PageNumber, friendshipParams.PageSize);

        Response.AddPaginationHeader(pagedFriends.CurrentPage, pagedFriends.PageSize, pagedFriends.TotalCount, pagedFriends.TotalPages);

        return Ok(pagedFriends);
    }

    // API: Lấy danh sách lời mời kết bạn ĐANG CHỜ XÁC NHẬN (Mình là người nhận)
    [HttpGet("requests/received")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] FriendshipParams friendshipParams)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var query = context.Friendships
            .Include(f => f.Requester)
            .Where(f => f.Status == FriendshipStatus.Pending && f.AddresseeId == currentUserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FriendUserResponse
            {
                UserId = f.Requester!.Id,
                FullName = f.Requester!.FullName,
                AvatarUrl = f.Requester!.AvatarUrl,
                Bio = f.Requester!.Bio,
                Status = f.Status,
                CreatedAt = f.CreatedAt
            });

        var pagedRequests = await PagedList<FriendUserResponse>.CreateAsync(query, friendshipParams.PageNumber, friendshipParams.PageSize);

        Response.AddPaginationHeader(pagedRequests.CurrentPage, pagedRequests.PageSize, pagedRequests.TotalCount, pagedRequests.TotalPages);

        return Ok(pagedRequests);
    }
}