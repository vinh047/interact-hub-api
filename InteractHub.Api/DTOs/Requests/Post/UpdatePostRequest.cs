using System.ComponentModel.DataAnnotations;
using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Requests.Post;

public class UpdatePostRequest
{
    [Required]
    public string? Content { get; set; }
    
    public PostVisibility? Visibility { get; set; }

    public List<IFormFile>? NewMediaFiles { get; set; }
    public List<Guid>? DeletedMediaIds { get; set; }
}