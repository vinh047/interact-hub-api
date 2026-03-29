using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs.Requests.Story;

public class CreateStoryRequest
{
    [Required(ErrorMessage = "Media URL is required.")]
    [Url(ErrorMessage = "Invalid Media URL format.")] // Validate định dạng link ảnh/video
    public required string MediaUrl { get; set; }
}