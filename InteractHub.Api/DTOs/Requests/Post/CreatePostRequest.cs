using System.ComponentModel.DataAnnotations;
using InteractHub.Api.Enums;

namespace  InteractHub.Api.DTOs.Requests.Post;

public class CreatePostRequest
{
    [Required(ErrorMessage = "Nội dung bài viết không được để trống")]
    [MaxLength(2000)]
    public required string Content { get; set; }

    // public IFormFile? Image { get; set; }

    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
}