using InteractHub.Api.Enums;

public class PostMediaResponse
{
    public Guid PostId { get; set; }
    public string? MediaUrl { get; set; }
    public MediaType MediaType { get; set; }
    public DateTime CreatedAt { get; set; }
}