using InteractHub.Api.Enums;

namespace InteractHub.Api.Entities;

public class Post : BaseEntity
{
    public required Guid UserId { get; set; }
    public string? Content { get; set; }
    // public string? ImageUrl { get; set; }
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    // Navigation Properties (Tạo khóa ngoại trỏ về User)
    public ApplicationUser? User { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Like> Likes { get; set; } = new List<Like>();

    public ICollection<PostMedia> MediaFiles { get; set; } = new List<PostMedia>();

    public ICollection<Hashtag> Hashtags { get; set; } = new List<Hashtag>();

}