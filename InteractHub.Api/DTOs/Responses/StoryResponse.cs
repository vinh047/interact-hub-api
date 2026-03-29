namespace InteractHub.Api.DTOs.Responses.Story;

public class StoryResponse
{
    public Guid Id { get; set; }
    public required string MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Thêm thời gian hết hạn để Frontend hiển thị thanh đếm ngược (ProgressBar)
    public DateTime ExpiresAt { get; set; }

    // Thông tin tác giả để render Avatar và Tên
    public Guid AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public string? AuthorAvatarUrl { get; set; }
}