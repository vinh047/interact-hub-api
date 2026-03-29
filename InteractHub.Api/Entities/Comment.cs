using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InteractHub.Api.Entities;

public class Comment : BaseEntity
{
    [Required]
    [MaxLength(1000)]
    public required string Content { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public Guid PostId { get; set; }
    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // Nếu là bình luận gốc: ParentCommentId = null
    // Nếu là câu trả lời (Reply): ParentCommentId = ID của bình luận cha
    public Guid? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public Comment? ParentComment { get; set; }

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}