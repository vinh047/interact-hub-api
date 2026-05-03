using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Helpers;

namespace InteractHub.Api.Services;

public interface IUserService
{
    Task<PagedList<UserResponse>> SearchUsersAsync(string keyword, int page, int limit, Guid currentUserId);
}