using System.ComponentModel.DataAnnotations;
using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Requests.Post;

public class CreatePostRequest
{
    [MaxLength(2000)]
    public string? Content { get; set; }

    // Cho phép upload nhiều file cùng lúc (Có thể null nếu bài viết chỉ có chữ)
    public List<IFormFile>? MediaFiles { get; set; }

    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
}