using InteractHub.Api.Enums;

public class UserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int FriendCount { get; set; }
    public FriendshipStatus? FriendshipStatus { get; set; }
    public bool? IsRequester { get; set; }
}