using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs.Requests.Comment;

public class UpdateCommentRequest
{
    [Required]
    public required string Content { get; set; }
}