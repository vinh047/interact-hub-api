using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Requests.Post;

public class ChangeVisibilityRequest
{
    public PostVisibility Visibility { get; set; }
}