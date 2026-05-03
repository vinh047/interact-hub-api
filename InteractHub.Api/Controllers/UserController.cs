using InteractHub.Api.Data;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InteractHub.Api.Controllers;

[Authorize]
public class UserController(ApplicationDbContext context, IFileService fileService, IUserService userService) : BaseController
{
    [HttpPatch("profile")]
    [Consumes("multipart/form-data")] // Chỉ định nhận dữ liệu có kèm file
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
    {
        // 1. Lấy UserId của người dùng hiện tại từ Token
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var userId = Guid.Parse(userIdClaim);

        // 2. Tìm User trong Database
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound("Không tìm thấy người dùng");

        // 3. Xử lý cập nhật thông tin văn bản
        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (request.Bio != null) // Cho phép Bio để trống (string rỗng)
            user.Bio = request.Bio;

        // 4. XỬ LÝ UPLOAD AVATAR (Nếu có file mới)
        if (request.AvatarFile != null)
        {
            try
            {
                // Gọi fileService của bạn để upload (giống cách làm ở Post)[cite: 5]
                var newAvatarUrl = await fileService.UploadFileAsync(request.AvatarFile, "avatars");

                // Cập nhật URL mới vào Database
                user.AvatarUrl = newAvatarUrl;
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi upload ảnh: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();

        // 5. Trả về Response chuẩn để FE cập nhật lại AuthContext
        return Ok(new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            // FriendCount = ... (Logic tính bạn bè của bạn)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        // Lấy ID của người đang đăng nhập từ BaseController
        var currentUserId = CurrentUserId;

        var user = await context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                FriendCount = u.SentFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted) +
                              u.ReceivedFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted),

                // Lấy trạng thái mối quan hệ giữa người xem và chủ profile
                FriendshipStatus = context.Friendships
                    .Where(f => (f.RequesterId == currentUserId && f.AddresseeId == id) ||
                                (f.RequesterId == id && f.AddresseeId == currentUserId))
                    .Select(f => (FriendshipStatus?)f.Status)
                    .FirstOrDefault(),

                // Kiểm tra xem người xem có phải là người gửi yêu cầu không
                IsRequester = context.Friendships
                    .Any(f => f.RequesterId == currentUserId && f.AddresseeId == id)
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            // Trả về list rỗng nếu không có từ khóa
            return Ok(new PagedList<UserResponse>(new List<UserResponse>(), 0, page, limit));
        }

        var users = await userService.SearchUsersAsync(q, page, limit, CurrentUserId);

        // Đính kèm Header phân trang cho Frontend
        Response.AddPaginationHeader(users.CurrentPage, users.Limit, users.TotalCount, users.TotalPages);

        return Ok(users);
    }
}