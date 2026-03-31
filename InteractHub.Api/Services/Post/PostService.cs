using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Requests.Post;
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
        var newPost = new Post { Content = request.Content, UserId = currentUserId };

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
        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == newPost.Id).MapToPostResponse(currentUserId).FirstAsync();
    }

    public async Task<PagedList<PostResponse>> GetNewsFeedAsync(PostQueryParameters queryParams, Guid currentUserId)
    {
        var query = context.Posts
            .Where(p => p.Visibility == PostVisibility.Public)
            .OrderByDescending(p => p.CreatedAt)
            .MapToPostResponse(currentUserId);

        return await PagedList<PostResponse>.CreateAsync(query, queryParams.PageNumber, queryParams.PageSize);
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
        var query = context.Posts.Where(p => p.UserId == targetUserId).OrderByDescending(p => p.CreatedAt).AsQueryable();

        if (currentUserId == targetUserId)
        {
            if (queryParams.Visibility.HasValue)
                query = query.Where(p => p.Visibility == queryParams.Visibility.Value);
        }
        else
        {
            query = query.Where(p => p.Visibility == PostVisibility.Public);
        }

        var selectQuery = query.MapToPostResponse(currentUserId);
        return await PagedList<PostResponse>.CreateAsync(selectQuery, queryParams.PageNumber, queryParams.PageSize);
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
        var post = await context.Posts.FindAsync(id);
        if (post == null) return null;

        if (post.UserId != currentUserId)
            throw new UnauthorizedAccessException("You do not have permission to update this post.");

        post.Content = request.Content;
        if (request.Visibility.HasValue) post.Visibility = request.Visibility.Value;
        post.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == post.Id).MapToPostResponse(currentUserId).FirstOrDefaultAsync();
    }
}