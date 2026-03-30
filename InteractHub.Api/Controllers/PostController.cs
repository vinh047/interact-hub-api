using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PostController(ApplicationDbContext context, IFileService fileService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        // Tạo Post rỗng (chỉ có chữ) trước
        var newPost = new Post
        {
            Content = request.Content,
            UserId = userId
        };

        // Xử lý danh sách File (nếu có)
        if (request.MediaFiles != null && request.MediaFiles.Any())
        {
            foreach (var file in request.MediaFiles)
            {
                // Gọi IFileService để lưu file và lấy Link URL
                var mediaUrl = await fileService.UploadFileAsync(file, "posts");

                // Đơn giản hóa việc check loại file (nếu ContentType bắt đầu bằng video/ thì là Video, ngược lại là Image)
                var mediaType = file.ContentType.StartsWith("video/") ? MediaType.Video : MediaType.Image;

                // Thêm vào danh sách Media của bài viết
                newPost.MediaFiles.Add(new PostMedia
                {
                    MediaUrl = mediaUrl,
                    MediaType = mediaType
                });
            }
        }

        context.Posts.Add(newPost);
        await context.SaveChangesAsync();

        var createdPost = await context.Posts
            .Include(p => p.User) // Lấy thông tin người đăng
            .Include(p => p.MediaFiles) // Lấy danh sách link ảnh/video vừa up
            .Where(p => p.Id == newPost.Id)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.UserId,
                AuthorName = p.User!.FullName,
                Visibility = p.Visibility,

                // Vừa đăng xong thì chắc chắn là 0 like, 0 comment
                CommentCount = 0,
                LikeCount = 0,
                IsLikedByCurrentUser = false,

                MediaFiles = p.MediaFiles.Select(m => new PostMediaResponse
                {
                    Id = m.Id,
                    MediaUrl = m.MediaUrl,
                    MediaType = m.MediaType.ToString()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return Ok(createdPost);
    }

    // API 2: Lấy danh sách bài viết (Mới nhất lên đầu)
    [HttpGet]
    public async Task<IActionResult> GetNewsFeed([FromQuery] PostQueryParameters queryParams)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var query = context.Posts
            .Where(p => p.Visibility == PostVisibility.Public)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.UserId,
                AuthorName = p.User!.FullName, // Tự động JOIN ở đây
                Visibility = p.Visibility,

                CommentCount = p.Comments.Count(),

                // Đếm tổng số lượt Like của bài viết
                LikeCount = p.Likes.Count(),
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),

                MediaFiles = p.MediaFiles.Select(m => new PostMediaResponse
                {
                    Id = m.Id,
                    MediaUrl = m.MediaUrl,
                    // Ép kiểu Enum sang chữ ("Image" hoặc "Video") để Frontend dễ đọc
                    MediaType = m.MediaType.ToString()
                }).ToList()
            });

        // 2. Chạy SQL và Đóng gói phân trang
        var pagedPosts = await PagedList<PostResponse>.CreateAsync(query, queryParams.PageNumber, queryParams.PageSize);

        Response.AddPaginationHeader(pagedPosts.CurrentPage, pagedPosts.PageSize, pagedPosts.TotalCount, pagedPosts.TotalPages);

        return Ok(pagedPosts);
    }

    // API 3: Xóa bài viết (Quy tắc: Chỉ tác giả mới được xóa)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var post = await context.Posts.FindAsync(id);

        if (post == null)
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        // Phân quyền: Kiểm tra xem người đang đăng nhập có phải là chủ bài viết không
        if (post.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to delete this post."));
        }

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(new { message = "Post deleted successfully." });
    }

    [HttpPatch("{id}/visibility")]
    public async Task<IActionResult> ChangeVisibility(Guid id, [FromBody] PostVisibility newVisibility)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var post = await context.Posts.FindAsync(id);

        if (post == null)
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        if (post.UserId != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to modify this post."));

        post.Visibility = newVisibility;
        await context.SaveChangesAsync();

        var updatedPost = await context.Posts
            .Where(p => p.Id == post.Id)
            .Select(p => new PostResponse
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
            })
            .FirstOrDefaultAsync();

        return Ok(updatedPost);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(Guid userId, [FromQuery] PostQueryParameters queryParams)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var query = context.Posts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (currentUserId == userId)
        {
            // -> Nếu Frontend có truyền bộ lọc Visibility lên, thì áp dụng lọc (để xem bài Private)
            if (queryParams.Visibility.HasValue)
            {
                query = query.Where(p => p.Visibility == queryParams.Visibility.Value);
            }
        }
        else
        {
            query = query.Where(p => p.Visibility == PostVisibility.Public);
        }

        var selectQuery = query.Select(p => new PostResponse
        {
            Id = p.Id,
            Content = p.Content,
            CreatedAt = p.CreatedAt,
            AuthorId = p.UserId,
            AuthorName = p.User!.FullName,
            Visibility = p.Visibility,

            CommentCount = p.Comments.Count(),

            // Đếm tổng số lượt Like của bài viết
            LikeCount = p.Likes.Count(),
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),

            MediaFiles = p.MediaFiles.Select(m => new PostMediaResponse
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                // Ép kiểu Enum sang chữ ("Image" hoặc "Video") để Frontend dễ đọc
                MediaType = m.MediaType.ToString()
            }).ToList()
        });

        var pagedPosts = await PagedList<PostResponse>.CreateAsync(selectQuery, queryParams.PageNumber, queryParams.PageSize);

        // Gắn Header phân trang
        Response.AddPaginationHeader(pagedPosts.CurrentPage, pagedPosts.PageSize, pagedPosts.TotalCount, pagedPosts.TotalPages);

        return Ok(pagedPosts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var post = await context.Posts
            .Where(p => p.Id == id)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                AuthorId = p.UserId,
                AuthorName = p.User!.FullName,
                Visibility = p.Visibility,

                CommentCount = p.Comments.Count(),

                // Đếm tổng số lượt Like của bài viết
                LikeCount = p.Likes.Count(),
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),

                MediaFiles = p.MediaFiles.Select(m => new PostMediaResponse
                {
                    Id = m.Id,
                    MediaUrl = m.MediaUrl,
                    // Ép kiểu Enum sang chữ ("Image" hoặc "Video") để Frontend dễ đọc
                    MediaType = m.MediaType.ToString()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        // Kiểm tra quyền xem bài Private
        if (post.Visibility == PostVisibility.Private && post.AuthorId != currentUserId)
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        return Ok(post);
    }

    // Cập nhật bài viết
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostRequest request)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == Guid.Empty)
            return Unauthorized(new ErrorResponse(ErrorCode.UNAUTHORIZED, "User identity not found."));

        var post = await context.Posts.FindAsync(id);

        if (post == null)
            return NotFound(new ErrorResponse(ErrorCode.POST_NOT_FOUND, "Post not found."));

        if (post.UserId != currentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(ErrorCode.FORBIDDEN_ACCESS, "You do not have permission to update this post."));

        post.Content = request.Content;

        if (request.Visibility.HasValue)
        {
            post.Visibility = request.Visibility.Value;
        }

        post.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var updatedPost = await context.Posts
            .Where(p => p.Id == post.Id)
            .Select(p => new PostResponse
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
            })
            .FirstOrDefaultAsync();

        return Ok(updatedPost);
    }
}