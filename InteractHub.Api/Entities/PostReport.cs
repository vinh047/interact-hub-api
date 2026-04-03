using InteractHub.Api.Enums;

namespace InteractHub.Api.Entities;

public class PostReport : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid ReporterId { get; set; }
    public ApplicationUser? Reporter { get; set; }

    public required string Reason { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
}