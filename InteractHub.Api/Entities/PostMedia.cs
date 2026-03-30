using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InteractHub.Api.Enums;

namespace InteractHub.Api.Entities;

public class PostMedia: BaseEntity
{
    [Required]
    public required string MediaUrl { get; set; }
    
    public MediaType MediaType { get; set; } = MediaType.Image;

    // Khóa ngoại nối về bảng Post
    public Guid PostId { get; set; }
    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }
}