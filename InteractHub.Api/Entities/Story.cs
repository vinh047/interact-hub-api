using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InteractHub.Api.Entities;

public class Story : BaseEntity 
{
    [Required]
    public required string MediaUrl { get; set; } // Chứa link ảnh hoặc video

    // Cột cực kỳ quan trọng: Thời điểm hết hạn (Thường là CreatedAt + 24h)
    public DateTime ExpiresAt { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
}