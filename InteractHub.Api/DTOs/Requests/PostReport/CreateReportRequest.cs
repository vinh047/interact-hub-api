using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs.Requests.PostReport;

public class CreateReportRequest
{
    [Required]
    public required string Reason { get; set; }
}