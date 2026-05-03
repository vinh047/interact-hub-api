using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Services;

public class UserService(ApplicationDbContext context) : IUserService
{
    // ... các hàm hiện tại của bạn

    public async Task<PagedList<UserResponse>> SearchUsersAsync(string keyword, int page, int limit, Guid currentUserId)
    {
        var query = context.Users.AsQueryable();

        // 1. Bỏ qua chính bản thân mình trong kết quả tìm kiếm
        query = query.Where(u => u.Id != currentUserId);

        // 2. Lọc theo từ khóa (Không phân biệt hoa thường)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // Lưu ý: Nếu bạn dùng PostgreSQL, hãy dùng EF.Functions.ILike(u.FullName, $"%{keyword}%") để tối ưu
            // Nếu dùng SQL Server, .Contains() mặc định đã không phân biệt hoa thường
            query = query.Where(u => u.FullName.ToLower().Contains(keyword.ToLower()));
        }

        // 3. Map sang UserResponse (Kèm theo trạng thái bạn bè giống GetProfile)
        var selectQuery = query.Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,
            Bio = u.Bio,
            // Đếm số lượng bạn bè
            FriendCount = u.SentFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted) +
                          u.ReceivedFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted),

            // Lấy trạng thái mối quan hệ giữa người tìm kiếm và user này
            FriendshipStatus = context.Friendships
                .Where(f => (f.RequesterId == currentUserId && f.AddresseeId == u.Id) ||
                            (f.RequesterId == u.Id && f.AddresseeId == currentUserId))
                .Select(f => (FriendshipStatus?)f.Status)
                .FirstOrDefault(),

            // Kiểm tra xem người tìm kiếm có phải là người đã gửi lời mời không[cite: 13]
            IsRequester = context.Friendships
                .Any(f => f.RequesterId == currentUserId && f.AddresseeId == u.Id)
        });

        // 4. Sắp xếp (Ưu tiên những người tên ngắn, khớp chính xác hơn lên đầu)
        selectQuery = selectQuery.OrderBy(u => u.FullName.Length).ThenBy(u => u.FullName);

        return await PagedList<UserResponse>.CreateAsync(selectQuery, page, limit);
    }
}