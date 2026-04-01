using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;

namespace InteractHub.Api.Extensions;

public static class CommentExtensions
{
    public static IQueryable<CommentResponse> MapToCommentResponse(this IQueryable<Comment> query, ApplicationDbContext context)
    {
        return query.Select(c => new CommentResponse
        {
            Id = c.Id,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            AuthorId = c.UserId,
            AuthorName = c.User!.FullName,
            AuthorAvatarUrl = c.User!.AvatarUrl,
            PostId = c.PostId,
            ParentCommentId = c.ParentCommentId,
            // Đếm xem có bao nhiêu bình luận con có ParentCommentId trỏ về ID của bình luận này
            ReplyCount = context.Comments.Count(r => r.ParentCommentId == c.Id)
        });
    }
}