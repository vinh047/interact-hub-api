namespace InteractHub.Api.Entities;

public class Friendship
{
    public required Guid RequesterId { get; set; }
    public required Guid AddresseeId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Blocked
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public ApplicationUser? Requester { get; set; }
    public ApplicationUser? Addressee { get; set; }
}