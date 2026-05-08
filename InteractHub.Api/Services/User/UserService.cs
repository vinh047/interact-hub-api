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

    public async Task<PagedList<UserResponse>> GetFriendSuggestionsAsync(int page, int limit, Guid currentUserId)
    {
        // 1. Lấy danh sách ID bạn bè HIỆN TẠI của người dùng (Đã Accepted)
        var currentUserFriendIds = await context.Friendships
            .Where(f => (f.RequesterId == currentUserId || f.AddresseeId == currentUserId) 
                        && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        // 2. Lấy danh sách ID những người ĐÃ CÓ tương tác (bạn bè, đang chờ duyệt, block...) để loại trừ khỏi gợi ý
        var existingRelationshipUserIds = await context.Friendships
            .Where(f => f.RequesterId == currentUserId || f.AddresseeId == currentUserId)
            .Select(f => f.RequesterId == currentUserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        // 3. Query người dùng lạ
        var query = context.Users
            .Where(u => u.Id != currentUserId && !existingRelationshipUserIds.Contains(u.Id));

        // 4. Map sang UserResponse và đếm bạn chung
        var selectQuery = query.Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,
            Bio = u.Bio,
            FriendCount = u.SentFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted) +
                          u.ReceivedFriendRequests.Count(f => f.Status == FriendshipStatus.Accepted),
            FriendshipStatus = null, 
            IsRequester = false,
            
            // ĐẾM BẠN CHUNG: Đếm số lượng tình bạn đã Accepted của người này (u.Id)
            // mà người kia (đối tác) nằm trong danh sách currentUserFriendIds
            MutualFriendsCount = context.Friendships.Count(f => 
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == u.Id && currentUserFriendIds.Contains(f.AddresseeId)) ||
                 (f.AddresseeId == u.Id && currentUserFriendIds.Contains(f.RequesterId))))
        });

        // 5. Ưu tiên những người có NHIỀU BẠN CHUNG NHẤT lên đầu, sau đó mới random
        selectQuery = selectQuery
            .OrderByDescending(u => u.MutualFriendsCount)
            .ThenBy(u => Guid.NewGuid());

        return await PagedList<UserResponse>.CreateAsync(selectQuery, page, limit);
    }
}