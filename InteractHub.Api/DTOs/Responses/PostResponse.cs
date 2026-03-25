using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Responses;

public class PostResponse
{
    public Guid Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Thông tin người đăng
    public Guid AuthorId { get; set; }
    public required string AuthorName { get; set; }

    public PostVisibility Visibility { get; set; }
}