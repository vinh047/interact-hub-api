using Microsoft.AspNetCore.Identity;

namespace InteractHub.Api.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Friendship> SentFriendRequests { get; set; } = [];
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = [];

    public ICollection<Like> Likes { get; set; } = new List<Like>();
}