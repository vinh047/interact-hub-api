using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Helpers;

public class PagedList<T> : List<T>
{
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int PageSize { get; private set; }
    public int TotalCount { get; private set; }

    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PagedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        AddRange(items); 
    }

    // Hàm tĩnh dùng chung cho mọi truy vấn (Hàm này bọc luôn lệnh chọc xuống Database)
    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, int pageNumber, int pageSize)
    {
        var count = await query.CountAsync(); // Đếm tổng số
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(); // Bốc dữ liệu
        
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}