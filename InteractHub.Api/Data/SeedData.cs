using System.Text.RegularExpressions;
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
        // 2. SEED USERS (50 Users)
        // ==========================================
        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName(u.FullName).Replace(".", "").Replace("_", "").ToLower() + f.Random.Number(100, 9999))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FullName))
            .RuleFor(u => u.AvatarUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Bio, f => f.Lorem.Sentence(5, 10));

        var users = userFaker.Generate(50);
        foreach (var user in users)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd123");
        }

        var userIds = await context.Users.Select(u => u.Id).ToListAsync();

        // ==========================================
        // 3. SEED POSTS (200 Bài viết) - ĐÃ THÊM HASHTAG
        // ==========================================
        var postFaker = new Faker<Post>()
            .RuleFor(p => p.UserId, f => f.PickRandom(userIds))
            .RuleFor(p => p.Content, f =>
            {
                // Tạo nội dung chữ bình thường
                var text = f.Lorem.Paragraphs(1, 2);

                // Sinh ngẫu nhiên từ 2 đến 5 từ, sau đó map thêm ký tự '#' ở trước
                var hashtags = string.Join(" ", f.Lorem.Words(f.Random.Int(2, 5)).Select(w => $"#{w}"));

                // Gộp lại với khoảng cách là 2 dấu xuống dòng
                return $"{text}\n\n{hashtags}";
            })
            .RuleFor(p => p.Visibility, f => f.PickRandom<PostVisibility>())
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(1).ToUniversalTime());

        var posts = postFaker.Generate(200);
        await context.Posts.AddRangeAsync(posts);
        await context.SaveChangesAsync(); // Lưu để lấy PostId cho các bảng dưới

        // ==========================================
        // 3.5. RÚT TRÍCH VÀ SEED HASHTAGS
        // ==========================================
        var hashtagsDict = new Dictionary<string, Hashtag>(StringComparer.OrdinalIgnoreCase);

        foreach (var post in posts)
        {
            var matches = Regex.Matches(post.Content, @"#(\w+)");
            var tagsInThisPost = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Khởi tạo danh sách Hashtags cho Post nếu chưa có
            if (post.Hashtags == null)
            {
                post.Hashtags = new List<Hashtag>();
            }

            foreach (Match match in matches)
            {
                var tagName = match.Groups[1].Value.ToLower();

                if (!tagsInThisPost.Add(tagName)) continue;

                if (!hashtagsDict.TryGetValue(tagName, out var hashtagEntity))
                {
                    hashtagEntity = new Hashtag
                    {
                        Name = tagName,
                        TrendingScore = 0 // Khởi tạo bằng 0 (nếu trong class bạn chưa set default)
                    };
                    hashtagsDict[tagName] = hashtagEntity;

                    // Thêm Hashtag mới vào DbContext
                    context.Hashtags.Add(hashtagEntity);
                }

                // NÂNG CẤP Ở ĐÂY: Mỗi lần hashtag được dùng, tăng điểm lên 1
                hashtagEntity.TrendingScore++;

                // MAGIC NẰM Ở ĐÂY: Chỉ cần Add thẳng vào danh sách của Post
                post.Hashtags.Add(hashtagEntity);
            }
        }

        await context.SaveChangesAsync();

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

            // B. Random từ 0 đến 15 comments
            int commentCount = faker.Random.Int(0, 15);
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
        // 5. SEED FRIENDSHIPS (500 Cặp bạn bè)
        // ==========================================
        var friendships = new List<Friendship>();
        var existingPairs = new HashSet<string>();

        for (int i = 0; i < 500; i++)
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
        // 6. SEED STORIES (50 Stories) - DẠNG MP4
        // ==========================================
        // Mảng các video mẫu có thật (public) để test giao diện hiển thị video
        var sampleMp4Videos = new[]
        {
            "https://interacthub.blob.core.windows.net/media/stories/20260504_020732_b246bb3c.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021104_c9a5465d.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021523_4867ecb4.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021810_a42b2c25.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021840_b8dc84eb.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021901_895cdb8a.mp4",
            "https://interacthub.blob.core.windows.net/media/stories/20260504_021922_dc6c98af.mp4",
        };

        var stories = new List<Story>();
        for (int i = 0; i < 50; i++)
        {
            var isExpired = faker.Random.Bool(0.3f); // 30% story đã hết hạn
            var createdAt = isExpired ? faker.Date.Recent(3).ToUniversalTime() : faker.Date.Recent(0).ToUniversalTime();

            stories.Add(new Story
            {
                UserId = faker.PickRandom(userIds),
                MediaUrl = faker.PickRandom(sampleMp4Videos), // Lấy random 1 video mp4 thật từ mảng trên
                CreatedAt = createdAt,
                ExpiresAt = createdAt.AddHours(24)
            });
        }
        await context.Stories.AddRangeAsync(stories);

        // LƯU TOÀN BỘ VÀO DB
        await context.SaveChangesAsync();
    }
}