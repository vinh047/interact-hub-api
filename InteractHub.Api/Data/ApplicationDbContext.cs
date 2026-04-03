using InteractHub.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PostReport> PostReports { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Bắt buộc phải có khi dùng Identity

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");

        // Xử lý khóa kép và chặn Cascade Delete cho bảng Friendship
        builder.Entity<Friendship>()
            .HasKey(f => new { f.RequesterId, f.AddresseeId });

        builder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Friendship>()
            .HasOne(f => f.Addressee)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);

        // User - Comment: Xóa User KHÔNG được xóa tự động Comment (Tránh Multiple Cascade Paths)
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post - Comment: Xóa Post thì XÓA LUÔN Comment
        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment - Comment (Tự tham chiếu): Xóa bình luận cha KHÔNG tự động xóa bình luận con dưới Database
        // Bắt buộc dùng Restrict ở đây vì SQL Server không cho phép Cascade Delete trên cùng 1 bảng
        builder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // CẤU HÌNH BẢNG LIKE
        // ==========================================

        builder.Entity<Like>().HasQueryFilter(l => !l.Post!.IsDeleted);

        // 1. Set Khóa chính kép (Composite Key) để chống 1 User like 1 bài 2 lần
        builder.Entity<Like>()
            .HasKey(l => new { l.PostId, l.UserId });

        // 2. User - Like: Xóa User thì KHÔNG tự động xóa Like (Tránh đụng độ vòng tròn)
        builder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 3. Post - Like: Xóa Bài viết thì XÓA LUÔN các lượt Like của bài đó
        builder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==========================================
        // CẤU HÌNH BẢNG STORY (TIN 24H)
        // ==========================================

        // 1. Cấu hình khóa ngoại (Xóa User thì xóa sạch Story của họ)
        builder.Entity<Story>()
            .HasOne(s => s.User)
            .WithMany(u => u.Stories)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 2. MAGIC TRICK: Global Query Filter tự động ẩn Story quá 24h
        // Khi Controller gọi lệnh lấy dữ liệu, EF Core sẽ tự động nối thêm điều kiện "Thời gian hết hạn > Thời gian hiện tại"
        builder.Entity<Story>().HasQueryFilter(s => s.ExpiresAt > DateTime.UtcNow);

        // ==========================================
        // CẤU HÌNH BẢNG POST MEDIA
        // ==========================================
        builder.Entity<PostMedia>().HasQueryFilter(pm => !pm.Post!.IsDeleted);

        builder.Entity<PostMedia>()
            .HasOne(pm => pm.Post)
            .WithMany(p => p.MediaFiles)
            .HasForeignKey(pm => pm.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1. Cấu hình Hashtag Name là Unique
        builder.Entity<Hashtag>()
            .HasIndex(h => h.Name)
            .IsUnique();

        // 2. Cấu hình Notification (Chống lỗi vòng lặp Cascade xóa User)
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications) // Nhớ thêm ICollection<Notification> Notifications vào ApplicationUser.cs nếu bạn muốn
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa User -> Xóa thông báo của họ

        builder.Entity<Notification>()
            .HasOne(n => n.Issuer)
            .WithMany() // Issuer không cần list Notification
            .HasForeignKey(n => n.IssuerId)
            .OnDelete(DeleteBehavior.Restrict); // Chống lỗi Cascade: Không cho xóa User nếu họ đang là Issuer của 1 thông báo

        // 3. Cấu hình PostReport
        builder.Entity<PostReport>()
            .HasOne(pr => pr.Reporter)
            .WithMany()
            .HasForeignKey(pr => pr.ReporterId)
            .OnDelete(DeleteBehavior.Restrict); // Không cho xóa User nếu họ đang có report

        builder.Entity<PostReport>()
            .HasOne(pr => pr.Post)
            .WithMany() // Post không cần lưu list Report
            .HasForeignKey(pr => pr.PostId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa bài viết -> Xóa luôn report của bài đó
    }
}