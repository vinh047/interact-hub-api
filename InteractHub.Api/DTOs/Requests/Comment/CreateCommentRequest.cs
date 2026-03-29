using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs.Requests.Comment;

public class CreateCommentRequest
{
    [Required]
    public required string Content { get; set; }

    [Required]
    public Guid PostId { get; set; }

    public Guid? ParentCommentId { get; set; } 
}