using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
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
public class CommentController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // kiểm tra post tồn tại chưa
        var postExists = await context.Posts.AnyAsync(p => p.Id == request.PostId);
        if (!postExists)
        {
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "The post you are trying to comment on does not exist or has been deleted."));
        }

        // Nếu là Reply, thì cái Bình luận cha có tồn tại không?
        if (request.ParentCommentId.HasValue)
        {
            // Phải đảm bảo bình luận cha đó thuộc về CHÍNH CÁI BÀI VIẾT NÀY. 
            var parentExists = await context.Comments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value && c.PostId == request.PostId);

            if (!parentExists)
            {
                return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "The parent comment does not exist in this post."));
            }
        }

        var newComment = new Comment
        {
            Content = request.Content,
            PostId = request.PostId,
            UserId = userId,
            ParentCommentId = request.ParentCommentId
        };

        context.Comments.Add(newComment);
        await context.SaveChangesAsync();

        var createdComment = await context.Comments
                    .Include(c => c.User)
                    .Where(c => c.Id == newComment.Id)
                    .Select(c => new CommentResponse
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
                        ReplyCount = 0 // Vừa tạo xong chắc chắn chưa có reply
                    })
                    .FirstOrDefaultAsync();

        return Ok(createdComment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentRequest request)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var comment = await context.Comments.FindAsync(id);

        if (comment == null)
        {
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Comment not found or has been deleted."));
        }

        // Chỉ chủ nhân của bình luận mới được phép sửa
        if (comment.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to edit this comment."));
        }

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var updatedComment = await context.Comments
            .Include(c => c.User)
            .Where(c => c.Id == comment.Id)
            .Select(c => new CommentResponse
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
                ReplyCount = context.Comments.Count(r => r.ParentCommentId == c.Id)
            })
            .FirstOrDefaultAsync();

        return Ok(updatedComment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var comment = await context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
        {
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Comment not found or already deleted."));
        }

        bool isCommentOwner = comment.UserId == currentUserId;
        bool isPostOwner = comment.Post!.UserId == currentUserId;

        if (!isCommentOwner && !isPostOwner)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to delete this comment."));
        }

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new { message = "Comment deleted successfully." });
    }

    [HttpGet("post/{postId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRootCommentsByPostId(Guid postId, [FromQuery] CommentParams commentParams)
    {
        var postExists = await context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
        {
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));
        }

        var query = context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentResponse
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

                ReplyCount = context.Comments.Count(r => r.ParentCommentId == c.Id),
            });

        var pagedComments = await PagedList<CommentResponse>.CreateAsync(
            query,
            commentParams.PageNumber,
            commentParams.PageSize
        );

        // 3. Gắn thông tin Phân trang vào HTTP Header (X-Pagination)
        Response.AddPaginationHeader(
            pagedComments.CurrentPage,
            pagedComments.PageSize,
            pagedComments.TotalCount,
            pagedComments.TotalPages
        );

        // 4. Trả về đúng cái danh sách dữ liệu siêu sạch (vì PagedList kế thừa từ List<T>)
        return Ok(pagedComments);
    }

    [HttpGet("{commentId}/replies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRepliesByCommentId(Guid commentId, [FromQuery] CommentParams commentParams)
    {
        // Kiểm tra xem bình luận cha có tồn tại không
        var commentExists = await context.Comments.AnyAsync(c => c.Id == commentId);
        if (!commentExists)
        {
            return NotFound(new ErrorResponse(ErrorCode.COMMENT_NOT_FOUND, "Parent comment not found."));
        }

        // Query lấy các reply (sâu 1 cấp)
        var query = context.Comments
            .Include(c => c.User)
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt) // Reply thường sắp xếp từ cũ tới mới để dễ đọc luồng hội thoại
            .Select(c => new CommentResponse
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
                ReplyCount = 0 // Thường các reply cấp 2 sẽ không có đếm reply con nữa
            });

        // Dùng PagedList giống hệt như lấy comment gốc
        var pagedReplies = await PagedList<CommentResponse>.CreateAsync(
            query,
            commentParams.PageNumber,
            commentParams.PageSize
        );

        Response.AddPaginationHeader(
            pagedReplies.CurrentPage,
            pagedReplies.PageSize,
            pagedReplies.TotalCount,
            pagedReplies.TotalPages
        );

        return Ok(pagedReplies);
    }
}