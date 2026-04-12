using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Extensions;
using InteractHub.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Services;

public class CommentService(ApplicationDbContext context) : ICommentService
{
    public async Task<CommentResponse> CreateCommentAsync(CreateCommentRequest request, Guid currentUserId)
    {
        // 1. Kiểm tra Post tồn tại
        var postExists = await context.Posts.AnyAsync(p => p.Id == request.PostId);
        if (!postExists)
            throw new KeyNotFoundException("The post you are trying to comment on does not exist or has been deleted.");

        // 2. Kiểm tra Parent Comment tồn tại (nếu có)
        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await context.Comments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value && c.PostId == request.PostId);

            if (!parentExists)
                throw new KeyNotFoundException("The parent comment does not exist in this post.");
        }

        // 3. Tạo mới
        var newComment = new Comment
        {
            Content = request.Content,
            PostId = request.PostId,
            UserId = currentUserId,
            ParentCommentId = request.ParentCommentId
        };

        context.Comments.Add(newComment);
        await context.SaveChangesAsync();

        // 4. Trả về Response (Dùng Extension Method siêu gọn)
        return await context.Comments
            .Include(c => c.User)
            .Where(c => c.Id == newComment.Id)
            .MapToCommentResponse(context)
            .FirstAsync();
    }

    public async Task<CommentResponse?> UpdateCommentAsync(Guid id, UpdateCommentRequest request, Guid currentUserId)
    {
        var comment = await context.Comments.FindAsync(id);
        if (comment == null) return null; // Trả về null để Controller ném 404

        // Middleware sẽ hứng cục này và dịch ra 403
        if (comment.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to edit this comment.");

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return await context.Comments
            .Include(c => c.User)
            .Where(c => c.Id == comment.Id)
            .MapToCommentResponse(context)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteCommentAsync(Guid id, Guid currentUserId)
    {
        var comment = await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null) return false;

        bool isCommentOwner = comment.UserId == currentUserId;
        bool isPostOwner = comment.Post!.UserId == currentUserId;

        // Chủ Comment HOẶC Chủ Post đều có quyền xóa
        if (!isCommentOwner && !isPostOwner)
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<PagedList<CommentResponse>?> GetRootCommentsByPostIdAsync(Guid postId, CommentParams commentParams)
    {
        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists) return null;

        var query = context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .MapToCommentResponse(context);

        return await PagedList<CommentResponse>.CreateAsync(query, commentParams.Page, commentParams.PageSize);
    }

    public async Task<PagedList<CommentResponse>?> GetRepliesByCommentIdAsync(Guid commentId, CommentParams commentParams)
    {
        var commentExists = await context.Comments.AnyAsync(c => c.Id == commentId);
        if (!commentExists) return null;

        var query = context.Comments
            .Include(c => c.User)
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt)
            .MapToCommentResponse(context);

        return await PagedList<CommentResponse>.CreateAsync(query, commentParams.Page, commentParams.PageSize);
    }
}