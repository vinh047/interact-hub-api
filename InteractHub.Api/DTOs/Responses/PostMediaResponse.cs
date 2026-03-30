namespace InteractHub.Api.DTOs.Responses;

public class PostMediaResponse
{
    public Guid Id { get; set; }
    public required string MediaUrl { get; set; }
    public string MediaType { get; set; } = string.Empty; // Trả về "Image" hoặc "Video"
}