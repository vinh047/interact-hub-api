namespace InteractHub.Api.Services;

public interface ILikeService
{
    // Trả về true nếu là Like (đã thêm), trả về false nếu là Unlike (đã gỡ)
    Task<bool> ToggleLikeAsync(Guid postId, Guid currentUserId);
}