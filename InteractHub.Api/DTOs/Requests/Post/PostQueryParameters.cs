using InteractHub.Api.Enums;

namespace InteractHub.Api.DTOs.Requests.Post;

public class PostQueryParameters : PaginationParams
{
    // Bộ lọc (Filter)
    public string? Sort { get; set; } = "desc"; // Sắp xếp theo CreatedAt, mặc định là mới nhất trước (desc)
    public PostVisibility? Visibility { get; set; } // Lọc theo trạng thái (tùy chọn)
}