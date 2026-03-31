using InteractHub.Api.DTOs.Requests.Post;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Enums;
using InteractHub.Api.Helpers;

namespace InteractHub.Api.Services;

public interface IPostService
{
    Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid currentUserId);
    Task<PagedList<PostResponse>> GetNewsFeedAsync(PostQueryParameters queryParams, Guid currentUserId);
    Task<bool> DeletePostAsync(Guid id, Guid currentUserId);
    Task<PostResponse?> ChangeVisibilityAsync(Guid id, PostVisibility newVisibility, Guid currentUserId);
    Task<PagedList<PostResponse>> GetUserPostsAsync(Guid targetUserId, PostQueryParameters queryParams, Guid currentUserId);
    Task<PostResponse?> GetPostByIdAsync(Guid id, Guid currentUserId);
    Task<PostResponse?> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid currentUserId);
}