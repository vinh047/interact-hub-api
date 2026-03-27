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
    // Nếu bảng User của bạn có Avatar thì sau này thêm vào đây:
    // public string? AuthorAvatar { get; set; }

    public Guid PostId { get; set; }

    // ID của bình luận cha (nếu có)
    public Guid? ParentCommentId { get; set; }

    // Chứa danh sách các câu trả lời cho bình luận này
    public List<CommentResponse> Replies { get; set; } = new List<CommentResponse>();
}