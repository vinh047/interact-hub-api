using InteractHub.Api.DTOs.Requests.Story;
using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Helpers;

namespace InteractHub.Api.Services;

public interface IStoryService
{
    Task<StoryResponse> CreateStoryAsync(CreateStoryRequest request, Guid currentUserId);
    Task<PagedList<StoryResponse>> GetStoryFeedAsync(StoryParams storyParams, Guid currentUserId);
    Task<bool> DeleteStoryAsync(Guid id, Guid currentUserId);
    Task<PagedList<StoryResponse>> GetStoryArchiveAsync(StoryParams storyParams, Guid currentUserId);
}