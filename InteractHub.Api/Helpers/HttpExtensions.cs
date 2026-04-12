using System.Text.Json;

namespace InteractHub.Api.Helpers;

public static class HttpExtensions
{
    public static void AddPaginationHeader(this HttpResponse response, int currentPage, int limit, int totalItems, int totalPages)
    {
        var paginationMetadata = new
        {
            currentPage,
            limit,
            totalItems,
            totalPages
        };

        // 1. Chuyển thành JSON và gắn vào Header
        response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
        
        // Phải có dòng này thì Frontend (React/Angular/Vue) mới đọc được Header custom.
        // Nếu không có, trình duyệt sẽ chặn lại vì lỗi CORS (Cross-Origin Resource Sharing).
        response.Headers.Append("Access-Control-Expose-Headers", "X-Pagination");
    }
}