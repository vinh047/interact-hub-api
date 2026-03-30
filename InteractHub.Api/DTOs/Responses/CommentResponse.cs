namespace InteractHub.Api.DTOs.Responses;

public class CommentResponse
{
    public Guid Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Thông tin người viết bình luận
    public Guid AuthorId { get; set; }
    public required string AuthorName { get; set; } 
    public string? AuthorAvatarUrl { get; set; } // Đã mở khóa Avatar

    public Guid PostId { get; set; }

    // ID của bình luận cha (nếu có)
    public Guid? ParentCommentId { get; set; }

    // Đếm số lượng reply (Giúp FE hiển thị chữ "Xem thêm 5 câu trả lời" mà chưa cần load list Replies)
    public int ReplyCount { get; set; }
}