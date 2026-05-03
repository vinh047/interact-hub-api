using System.Text.RegularExpressions;
using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Requests.PostMedia;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Services;

public class PostService(ApplicationDbContext context, IFileService fileService) : IPostService
{
    public async Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid currentUserId)
    {
        var newPost = new Post { Content = request.Content ?? string.Empty, UserId = currentUserId, Visibility = request.Visibility };

        if (request.MediaFiles != null && request.MediaFiles.Any())
        {
            foreach (var file in request.MediaFiles)
            {
                var mediaUrl = await fileService.UploadFileAsync(file, "posts");
                var mediaType = file.ContentType.StartsWith("video/") ? MediaType.Video : MediaType.Image;
                newPost.MediaFiles.Add(new PostMedia { MediaUrl = mediaUrl, MediaType = mediaType });
            }
        }

        context.Posts.Add(newPost);

        var extractedTags = ExtractHashtags(request.Content);

        foreach (var tagName in extractedTags)
        {
            var existingHashtag = await context.Hashtags.FirstOrDefaultAsync(h => h.Name == tagName);

            if (existingHashtag == null)
            {
                existingHashtag = new Hashtag { Name = tagName, TrendingScore = 1 };
                context.Hashtags.Add(existingHashtag);
            }
            else
            {
                existingHashtag.TrendingScore += 1;
            }

            newPost.Hashtags.Add(existingHashtag);
        }

        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == newPost.Id).MapToPostResponse(currentUserId).FirstAsync();
    }

    public async Task<PagedList<PostResponse>> GetNewsFeedAsync(PostQueryParameters queryParams, Guid currentUserId)
    {
        var query = context.Posts
            .Where(p =>
            // 1. Luôn lấy tất cả bài viết Public
            p.Visibility == PostVisibility.Public ||

            // 2. Lấy tất cả bài viết của chính user đang đăng nhập (để họ thấy bài Private/FriendsOnly của mình)
            p.UserId == currentUserId ||

            // 3. Xử lý bài FriendsOnly: Chỉ lấy nếu user hiện tại và người đăng bài là bạn bè
            (p.Visibility == PostVisibility.FriendsOnly && context.Friendships.Any(f =>

                // Trạng thái kết bạn phải là đã chấp nhận (Giả định Enum của bạn có giá trị là Accepted)
                f.Status == FriendshipStatus.Accepted &&

                // Kiểm tra quan hệ 2 chiều: 
                // Chiều 1: User hiện tại là người gửi (Requester) và người đăng bài là người nhận (Addressee)
                ((f.RequesterId == currentUserId && f.AddresseeId == p.UserId) ||

                 // Chiều 2: Người đăng bài là người gửi (Requester) và User hiện tại là người nhận (Addressee)
                 (f.RequesterId == p.UserId && f.AddresseeId == currentUserId))
            ))
        )
            .OrderByDescending(p => p.CreatedAt)
            .MapToPostResponse(currentUserId);

        return await PagedList<PostResponse>.CreateAsync(query, queryParams.Page, queryParams.Limit);
    }

    public async Task<bool> DeletePostAsync(Guid id, Guid currentUserId)
    {
        var post = await context.Posts.FindAsync(id);
        if (post == null) return false; // Trả về false nếu không tìm thấy (404)

        if (post.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to delete this post."); // Ném lỗi (403)

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<PostResponse?> ChangeVisibilityAsync(Guid id, PostVisibility newVisibility, Guid currentUserId)
    {
        var post = await context.Posts.FindAsync(id);
        if (post == null) return null;

        if (post.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to modify this post.");

        post.Visibility = newVisibility;
        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == post.Id).MapToPostResponse(currentUserId).FirstOrDefaultAsync();
    }

    public async Task<PagedList<PostResponse>> GetUserPostsAsync(Guid targetUserId, PostQueryParameters queryParams, Guid currentUserId)
    {
        // 1. Khởi tạo query lọc theo User ID
        var query = context.Posts.Where(p => p.UserId == targetUserId).AsQueryable();

        // 2. Xử lý Visibility (Quyền riêng tư)
        if (currentUserId == targetUserId)
        {
            // Nếu là chủ nhà: Lọc theo yêu cầu hoặc lấy hết
            if (queryParams.Visibility.HasValue)
                query = query.Where(p => p.Visibility == queryParams.Visibility.Value);
        }
        else
        {
            // Kiểm tra quan hệ bạn bè (Logic nâng cao chúng ta đã bàn)
            bool isFriend = await context.Friendships.AnyAsync(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == currentUserId && f.AddresseeId == targetUserId) ||
                 (f.RequesterId == targetUserId && f.AddresseeId == currentUserId))
            );

            if (isFriend)
                query = query.Where(p => p.Visibility == PostVisibility.Public || p.Visibility == PostVisibility.FriendsOnly);
            else
                query = query.Where(p => p.Visibility == PostVisibility.Public);
        }

        // 3. XỬ LÝ SẮP XẾP (Đặt ở đây để đảm bảo không bị ghi đè)
        if (queryParams.Sort?.ToLower() == "asc")
        {
            query = query.OrderBy(p => p.CreatedAt);
        }
        else
        {
            query = query.OrderByDescending(p => p.CreatedAt);
        }

        // 4. Map sang Response DTO
        var selectQuery = query.MapToPostResponse(currentUserId);

        // 5. Thực thi phân trang
        return await PagedList<PostResponse>.CreateAsync(selectQuery, queryParams.Page, queryParams.Limit);
    }

    public async Task<PostResponse?> GetPostByIdAsync(Guid id, Guid currentUserId)
    {
        var post = await context.Posts.Where(p => p.Id == id).MapToPostResponse(currentUserId).FirstOrDefaultAsync();
        if (post == null) return null;

        if (post.Visibility == PostVisibility.Private && post.AuthorId != currentUserId)
            return null;

        return post;
    }

    public async Task<PostResponse?> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid currentUserId)
    {
        var post = await context.Posts
            .Include(p => p.MediaFiles)
            .Include(p => p.Hashtags) // QUAN TRỌNG: Phải Include Hashtags để EF Core biết bài này đang có tag nào
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return null;

        if (post.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to update this post.");

        // 1. Cập nhật các trường văn bản
        post.Content = request.Content;
        if (request.Visibility.HasValue) post.Visibility = request.Visibility.Value;
        post.UpdatedAt = DateTime.UtcNow;

        // --- 2. XỬ LÝ HASHTAG ---
        // Lấy danh sách hashtag mới từ Content vừa update
        var newTags = ExtractHashtags(request.Content);
        var currentTags = post.Hashtags.ToList();

        // 2.1 Xóa những Tag cũ không còn nằm trong content mới
        var tagsToRemove = currentTags.Where(t => !newTags.Contains(t.Name)).ToList();
        foreach (var tag in tagsToRemove)
        {
            tag.TrendingScore = Math.Max(0, tag.TrendingScore - 1); // Giảm điểm hot (không cho âm)
            post.Hashtags.Remove(tag); // EF Core sẽ tự động xóa dòng trong bảng trung gian PostHashtags
        }

        // 2.2 Thêm những Tag mới xuất hiện
        var currentTagNames = currentTags.Select(t => t.Name).ToList();
        var tagsToAdd = newTags.Where(t => !currentTagNames.Contains(t)).ToList();

        foreach (var tagName in tagsToAdd)
        {
            var existingHashtag = await context.Hashtags.FirstOrDefaultAsync(h => h.Name == tagName);
            if (existingHashtag == null)
            {
                existingHashtag = new Hashtag { Name = tagName, TrendingScore = 1 };
                context.Hashtags.Add(existingHashtag);
            }
            else
            {
                existingHashtag.TrendingScore += 1; // Tăng điểm hot
            }

            post.Hashtags.Add(existingHashtag); // Add vào collection để EF Core tạo link Many-to-Many
        }
        // --- KẾT THÚC XỬ LÝ HASHTAG ---

        // 3. XỬ LÝ XÓA MEDIA CŨ
        if (request.DeletedMediaIds != null && request.DeletedMediaIds.Any())
        {
            var mediaToRemove = post.MediaFiles
                                    .Where(m => request.DeletedMediaIds.Contains(m.Id))
                                    .ToList();
            if (mediaToRemove.Any())
            {
                context.PostMedia.RemoveRange(mediaToRemove);
            }
        }

        // 4. XỬ LÝ THÊM MEDIA MỚI
        if (request.NewMediaFiles != null && request.NewMediaFiles.Any())
        {
            foreach (var file in request.NewMediaFiles)
            {
                var mediaUrl = await fileService.UploadFileAsync(file, "posts");
                var mediaType = file.ContentType.StartsWith("video/") ? MediaType.Video : MediaType.Image;

                var newMedia = new PostMedia
                {
                    PostId = post.Id,
                    MediaUrl = mediaUrl,
                    MediaType = mediaType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.PostMedia.Add(newMedia);
            }
        }

        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == post.Id).MapToPostResponse(currentUserId).FirstOrDefaultAsync();
    }

    public async Task<PagedList<PostMediaResponse>> GetUserMediaAsync(Guid targetUserId, Guid currentUserId, PostMediaQueryParameters paginationParams)
    {
        // 1. Kiểm tra mối quan hệ để xác định quyền xem bài viết[cite: 4]
        var friendship = await context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == currentUserId && f.AddresseeId == targetUserId) ||
                (f.RequesterId == targetUserId && f.AddresseeId == currentUserId));

        bool isOwner = currentUserId == targetUserId;
        bool isFriend = friendship?.Status == FriendshipStatus.Accepted;

        // 2. Truy vấn từ bảng PostMedia, kết hợp với Post để lọc
        var query = context.PostMedia
            .Include(m => m.Post)
            .Where(m => m.Post!.UserId == targetUserId && !m.Post.IsDeleted) // Lọc theo UserId của bài viết
            .Where(m => isOwner
                ? true
                : isFriend
                    ? m.Post!.Visibility != PostVisibility.Private
                    : m.Post!.Visibility == PostVisibility.Public) // Kiểm tra Visibility[cite: 9]
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new PostMediaResponse
            {
                PostId = m.PostId,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                CreatedAt = m.CreatedAt
            });

        return await PagedList<PostMediaResponse>.CreateAsync(query, paginationParams.Page, paginationParams.Limit);
    }

    public async Task<PagedList<PostResponse>> SearchPostsAsync(string keyword, int page, int limit, Guid currentUserId)
    {
        // 1. Khởi tạo query và loại bỏ bài đã xóa
        var query = context.Posts.Where(p => !p.IsDeleted).AsQueryable();

        // 2. Lọc theo từ khóa (nếu có)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p => p.Content!.ToLower().Contains(keyword.ToLower()));
        }

        // 3. LOGIC PHÂN QUYỀN (Sao chép tư duy từ NewsFeed)
        query = query.Where(p =>
            // Bài Public
            p.Visibility == PostVisibility.Public ||

            // Bài của chính mình
            p.UserId == currentUserId ||

            // Bài FriendsOnly: Chỉ lấy nếu 2 người là bạn bè
            (p.Visibility == PostVisibility.FriendsOnly && context.Friendships.Any(f =>
                f.Status == FriendshipStatus.Accepted &&
                ((f.RequesterId == currentUserId && f.AddresseeId == p.UserId) ||
                 (f.RequesterId == p.UserId && f.AddresseeId == currentUserId))
            ))
        );

        // 4. Sắp xếp kết quả: Bài mới nhất lên đầu
        query = query.OrderByDescending(p => p.CreatedAt);

        // 5. Map sang Response DTO và thực thi phân trang
        var selectQuery = query.MapToPostResponse(currentUserId);

        return await PagedList<PostResponse>.CreateAsync(selectQuery, page, limit);
    }

    private List<string> ExtractHashtags(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();

        // Bóc tách hashtag chuẩn Unicode
        var regex = new Regex(@"#[\p{L}\p{N}_]+", RegexOptions.Compiled);
        var matches = regex.Matches(content);

        return matches
            .Select(m => m.Value.Substring(1).ToLower())
            .Distinct()
            .ToList();
    }


}