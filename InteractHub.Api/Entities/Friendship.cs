using System.ComponentModel.DataAnnotations.Schema;
using InteractHub.Api.Enums;

namespace InteractHub.Api.Entities;

public class Friendship
{
    // Người gửi lời mời kết bạn (Người chủ động)
    public required Guid RequesterId { get; set; }
    [ForeignKey(nameof(RequesterId))]
    public ApplicationUser? Requester { get; set; }

    // Người nhận lời mời kết bạn (Người bị động)
    public required Guid AddresseeId { get; set; }
    [ForeignKey(nameof(AddresseeId))]
    public ApplicationUser? Addressee { get; set; }

    // Trạng thái của mối quan hệ
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}