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
            .WithMany()
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
    }
}