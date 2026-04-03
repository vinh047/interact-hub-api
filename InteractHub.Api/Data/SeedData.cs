using Bogus;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Api.Data;

public class SeedData
{
    public static async Task SeedDatabaseAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // 1. DỪNG NẾU ĐÃ CÓ DATA
        if (await userManager.Users.AnyAsync()) return;

        var adminUser = new ApplicationUser
        {
            FullName = "Admin User",
            UserName = "user",
            Email = "user@gmail.com"
        };
        await userManager.CreateAsync(adminUser, "123456");

        // ==========================================
        // 2. SEED USERS (20 Users)
        // ==========================================
        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName(u.FullName).Replace(".", "").Replace("_", "").ToLower() + f.Random.Number(100, 9999))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FullName))
            .RuleFor(u => u.AvatarUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Bio, f => f.Lorem.Sentence(5, 10));

        var users = userFaker.Generate(20);
        foreach (var user in users)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd123");
        }

        var userIds = await context.Users.Select(u => u.Id).ToListAsync();

        // ==========================================
        // 3. SEED POSTS (50 Bài viết)
        // ==========================================
        var postFaker = new Faker<Post>()
            .RuleFor(p => p.UserId, f => f.PickRandom(userIds))
            .RuleFor(p => p.Content, f => f.Lorem.Paragraphs(1, 3))
            .RuleFor(p => p.Visibility, f => f.PickRandom<PostVisibility>())
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(1).ToUniversalTime());

        var posts = postFaker.Generate(50);
        await context.Posts.AddRangeAsync(posts);
        await context.SaveChangesAsync(); // Lưu để lấy PostId cho các bảng dưới

        // ==========================================
        // 4. SEED POST MEDIA, COMMENTS & LIKES
        // ==========================================
        var postMedias = new List<PostMedia>();
        var comments = new List<Comment>();
        var likes = new List<Like>();
        var faker = new Faker();

        foreach (var post in posts)
        {
            // A. Random từ 0 đến 3 ảnh cho mỗi bài viết
            int mediaCount = faker.Random.Int(0, 3);
            for (int i = 0; i < mediaCount; i++)
            {
                postMedias.Add(new PostMedia
                {
                    PostId = post.Id,
                    MediaUrl = faker.Image.PicsumUrl(),
                    MediaType = 0, // Giả sử 0 là Image, nếu bạn dùng Enum thì đổi thành MediaType.Image
                    CreatedAt = post.CreatedAt.AddMinutes(faker.Random.Int(1, 5))
                });
            }

            // B. Random từ 0 đến 5 comments
            int commentCount = faker.Random.Int(0, 5);
            for (int i = 0; i < commentCount; i++)
            {
                comments.Add(new Comment
                {
                    PostId = post.Id,
                    UserId = faker.PickRandom(userIds),
                    Content = faker.Lorem.Sentence(),
                    CreatedAt = post.CreatedAt.AddHours(faker.Random.Int(1, 48))
                });
            }

            // C. Random Likes (Chống trùng lặp User like 2 lần)
            int likeCount = faker.Random.Int(0, 10);
            var randomUsersToLike = faker.PickRandom(userIds, likeCount).Distinct();
            foreach (var userId in randomUsersToLike)
            {
                likes.Add(new Like
                {
                    PostId = post.Id,
                    UserId = userId,
                    CreatedAt = post.CreatedAt.AddMinutes(faker.Random.Int(10, 100))
                });
            }
        }

        await context.PostMedia.AddRangeAsync(postMedias);
        await context.Comments.AddRangeAsync(comments);
        await context.Likes.AddRangeAsync(likes);

        // ==========================================
        // 5. SEED FRIENDSHIPS (30 Cặp bạn bè)
        // ==========================================
        var friendships = new List<Friendship>();
        var existingPairs = new HashSet<string>();

        for (int i = 0; i < 30; i++)
        {
            var requesterId = faker.PickRandom(userIds);
            var addresseeId = faker.PickRandom(userIds);

            // Chống tự kết bạn với chính mình
            if (requesterId == addresseeId) continue;

            // Tạo key để check trùng (bất chấp chiều gửi)
            var pairKey = string.Compare(requesterId.ToString(), addresseeId.ToString()) < 0
                ? $"{requesterId}-{addresseeId}"
                : $"{addresseeId}-{requesterId}";

            if (existingPairs.Add(pairKey)) // Nếu Add thành công (chưa từng tồn tại)
            {
                friendships.Add(new Friendship
                {
                    RequesterId = requesterId,
                    AddresseeId = addresseeId,
                    Status = faker.Random.Bool(0.7f) ? FriendshipStatus.Accepted : FriendshipStatus.Pending, // 70% là bạn, 30% chờ
                    CreatedAt = faker.Date.Past(1).ToUniversalTime()
                });
            }
        }
        await context.Friendships.AddRangeAsync(friendships);

        // ==========================================
        // 6. SEED STORIES (20 Stories)
        // ==========================================
        var stories = new List<Story>();
        for (int i = 0; i < 20; i++)
        {
            var isExpired = faker.Random.Bool(0.3f); // 30% story đã hết hạn
            var createdAt = isExpired ? faker.Date.Recent(3).ToUniversalTime() : faker.Date.Recent(0).ToUniversalTime();

            stories.Add(new Story
            {
                UserId = faker.PickRandom(userIds),
                MediaUrl = faker.Image.PicsumUrl(),
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(24)
            });
        }
        await context.Stories.AddRangeAsync(stories);

        // LƯU TOÀN BỘ VÀO DB
        await context.SaveChangesAsync();
    }
}