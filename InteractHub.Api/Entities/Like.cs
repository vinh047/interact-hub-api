using System.ComponentModel.DataAnnotations.Schema;

namespace InteractHub.Api.Entities;

public class Like
{
    public Guid PostId { get; set; }
    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}