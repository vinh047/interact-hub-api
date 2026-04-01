using InteractHub.Api.DTOs.Responses.Story;
using InteractHub.Api.Entities;

namespace InteractHub.Api.Extensions;

public static class StoryExtensions
{
    public static IQueryable<StoryResponse> MapToStoryResponse(this IQueryable<Story> query)
    {
        return query.Select(s => new StoryResponse
        {
            Id = s.Id,
            MediaUrl = s.MediaUrl,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            AuthorId = s.UserId,
            AuthorName = s.User!.FullName,
            AuthorAvatarUrl = s.User!.AvatarUrl
        });
    }
}