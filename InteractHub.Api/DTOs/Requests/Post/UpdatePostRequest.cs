using System.ComponentModel.DataAnnotations;
using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Requests.Post;

public class UpdatePostRequest
{
    [Required]
    public required string Content { get; set; }
    
    public PostVisibility? Visibility { get; set; }
}