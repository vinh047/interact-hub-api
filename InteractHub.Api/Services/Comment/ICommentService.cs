using InteractHub.Api.DTOs.Requests.Comment;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Helpers;

namespace InteractHub.Api.Services;

public interface ICommentService
{
    Task<CommentResponse> CreateCommentAsync(CreateCommentRequest request, Guid currentUserId);
    Task<CommentResponse?> UpdateCommentAsync(Guid id, UpdateCommentRequest request, Guid currentUserId);
    Task<bool> DeleteCommentAsync(Guid id, Guid currentUserId);
    Task<PagedList<CommentResponse>?> GetRootCommentsByPostIdAsync(Guid postId, CommentParams commentParams);
    Task<PagedList<CommentResponse>?> GetRepliesByCommentIdAsync(Guid commentId, CommentParams commentParams);
}