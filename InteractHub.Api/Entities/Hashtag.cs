namespace InteractHub.Api.Entities;

public class Hashtag : BaseEntity
{
    public required string Name { get; set; } // Sẽ được cấu hình Unique (Duy nhất) trong DbContext
    public int TrendingScore { get; set; } = 0;

    // Quan hệ Many-to-Many với Post (EF Core 8 sẽ tự động tạo bảng trung gian PostHashtag)
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}