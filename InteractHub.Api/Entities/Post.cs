namespace InteractHub.Api.Entities;

public class Post : BaseEntity
{
    public required Guid UserId { get; set; }
    public required string Content { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation Properties (Tạo khóa ngoại trỏ về User)
    public ApplicationUser? User { get; set; }
}