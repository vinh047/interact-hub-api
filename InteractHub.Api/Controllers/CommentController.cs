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

        return Ok(new { message = "Comment created successfully.", commentId = newComment.Id });
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

        return Ok(new { message = "Comment updated successfully." });
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
    public async Task<IActionResult> GetPostComments(Guid postId)
    {
        var allComments = await context.Comments
            .IgnoreQueryFilters()
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (!allComments.Any())
        {
            return Ok(new List<CommentResponse>()); // Trả về mảng rỗng nếu chưa có bình luận nào
        }

        var commentDictionary = new Dictionary<Guid, CommentResponse>();
        var rootComments = new List<CommentResponse>();

        foreach (var comment in allComments)
        {
            var response = new CommentResponse
            {
                Id = comment.Id,
                // NẾU BỊ XÓA: Che giấu nội dung thật
                Content = comment.IsDeleted ? "[Bình luận này đã bị xóa]" : comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                PostId = comment.PostId,
                ParentCommentId = comment.ParentCommentId,

                // NẾU BỊ XÓA: Che giấu danh tính người viết để bảo vệ quyền riêng tư
                AuthorId = comment.IsDeleted ? Guid.Empty : comment.UserId,
                AuthorName = comment.IsDeleted ? "Người dùng ẩn danh" : (comment.User?.FullName ?? "Unknown"),
                Replies = new List<CommentResponse>()
            };

            commentDictionary.Add(response.Id, response);
        }

        // 2.2 - LẮP RÁP CÂY (Thuật toán ghép con vào cha)
        foreach (var comment in allComments)
        {
            var dto = commentDictionary[comment.Id];

            // Nếu nó có Cha, và Cha của nó có tồn tại trong bài viết này
            if (comment.ParentCommentId.HasValue && commentDictionary.ContainsKey(comment.ParentCommentId.Value))
            {
                // Bỏ đứa con vào túi (Replies) của người cha
                commentDictionary[comment.ParentCommentId.Value].Replies.Add(dto);
            }
            else
            {
                // Nếu không có Cha (ParentCommentId == null) -> Nó là Rễ cây (Root)
                rootComments.Add(dto);
            }
        }

        // BƯỚC 3: DỌN DẸP RÁC BẰNG ĐỆ QUY
        // Sẽ có những bình luận đã bị xóa, và TÌNH CỜ nó cũng chẳng có đứa con nào bám vào.
        // Trả mấy cái bình luận đó về Frontend chỉ tổ chật chỗ, nên ta dùng hàm đệ quy để tỉa chúng đi.
        PruneDeletedComments(rootComments);

        return Ok(rootComments);
    }

    private void PruneDeletedComments(List<CommentResponse> comments)
    {
        for (int i = comments.Count - 1; i >= 0; i--)
        {
            var comment = comments[i];

            if (comment.Replies.Any())
            {
                PruneDeletedComments(comment.Replies);
            }

            if (comment.Content == "[Bình luận này đã bị xóa]" && !comment.Replies.Any())
            {
                comments.RemoveAt(i);
            }
        }
    }
}