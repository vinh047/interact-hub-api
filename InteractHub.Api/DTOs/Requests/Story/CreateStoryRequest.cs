using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs.Requests.Story;

public class CreateStoryRequest
{
    [Required(ErrorMessage = "Media URL is required.")]
    public required IFormFile MediaFile { get; set; }
}