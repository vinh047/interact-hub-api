using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;

namespace InteractHub.Api.Extensions;

public static class PostExtensions
{
    // Từ khóa 'this' ở tham số đầu tiên biến hàm này thành hàm mở rộng cho IQueryable<Post>
    public static IQueryable<PostResponse> MapToPostResponse(this IQueryable<Post> query, Guid currentUserId)
    {
        return query.Select(p => new PostResponse
        {
            Id = p.Id,
            Content = p.Content,
            CreatedAt = p.CreatedAt,
            AuthorId = p.UserId,
            AuthorName = p.User!.FullName,
            Visibility = p.Visibility,
            
            CommentCount = p.Comments.Count(),
            LikeCount = p.Likes.Count(),
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            
            MediaFiles = p.MediaFiles.Select(m => new PostMediaResponse
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType.ToString()
            }).ToList()
        });
    }
}