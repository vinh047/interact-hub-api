namespace InteractHub.Api.DTOs.Requests.Friendship;

public class FriendshipParams : PaginationParams
{
    public Guid? UserId { get; set; }
    public string? Search { get; set; }
}