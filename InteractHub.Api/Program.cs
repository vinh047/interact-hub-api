using System.Text;
using System.Text.Json;
using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Middlewares;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Identity (Bắt buộc để xài UserManager và RoleManager)
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // 1. Cấu hình Mật khẩu (Password)
    options.Password.RequireDigit = false;            // Không bắt buộc có số
    options.Password.RequiredLength = 6;             // Độ dài tối thiểu (mặc định là 6)
    options.Password.RequireNonAlphanumeric = false; // Không bắt buộc ký tự đặc biệt (@, #, !)
    options.Password.RequireUppercase = false;       // Không bắt buộc chữ hoa
    options.Password.RequireLowercase = false;       // Không bắt buộc chữ thường

    // 2. Cấu hình User
    options.User.RequireUniqueEmail = true;          // Bắt buộc Email là duy nhất
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },

        OnChallenge = async context =>
        {
            // 1. Tắt phản hồi 401 rỗng mặc định của Microsoft
            context.HandleResponse();

            // 2. Thiết lập Response trả về kiểu JSON và mã lỗi 401
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            // 3. Đóng gói lỗi bằng "Chiếc hộp" ErrorResponse chuẩn của dự án
            var errorResponse = new ErrorResponse(
                ErrorCode.UNAUTHORIZED,
                "Please login or provide a valid token."
            );

            // 4. Biến Object thành chuỗi JSON (camelCase mặc định) và gửi về Frontend
            var jsonResult = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResult);
        }
    };
});

// Thêm dịch vụ Controllers
builder.Services.AddControllers()
.ConfigureApiBehaviorOptions(options =>
    {
        // Ghi đè hành vi trả về lỗi mặc định của Model Validation
        options.InvalidModelStateResponseFactory = context =>
        {
            // 1. Thu thập tất cả các lỗi từ DataAnnotations ([Required], [MaxLength]...)
            var errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key, // Tên field bị lỗi (VD: "Content")
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray() // Danh sách câu thông báo lỗi
                );

            // 2. Gói nó vào trong ErrorResponse chuẩn của bạn
            var errorResponse = new ErrorResponse(
                ErrorCode.BAD_REQUEST,
                "Invalid input data. Please check your request and try again.",
                errors
            );

            // 3. Trả về mã 400 Bad Request kèm theo cục JSON xịn xò này
            return new BadRequestObjectResult(errorResponse);
        };
    });

builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký HttpContextAccessor để lấy được Base URL (https://localhost:...)
builder.Services.AddHttpContextAccessor();

// Đăng ký dịch vụ FileService (Plug & Play)
// builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<IFileService, AzureBlobFileService>();

builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Cổng của Vite/React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Cho phép gửi Cookie/Token nếu cần
              .WithExposedHeaders("X-Pagination");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

// THÊM DÒNG NÀY ĐỂ MỞ CỬA CHO PHÉP TRUY CẬP ẢNH TỪ THƯ MỤC wwwroot
app.UseStaticFiles();

app.UseCors("AllowReactApp");

// Kích hoạt Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map các đường dẫn API tới các Controller
app.MapControllers();

// ================= BẮT ĐẦU ĐOẠN SEED DATA =================
// Tạo một scope tạm thời để lấy các Service (DbContext, UserManager) ra dùng
if (app.Environment.IsDevelopment() && args.Contains("--seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Xóa sạch Database hiện tại (Cực kỳ mạnh mẽ!)
            Console.WriteLine("--> Đang xóa Database cũ...");
            await context.Database.EnsureDeletedAsync();

            // 2. Chạy lại các Migration để tạo cấu trúc bảng mới nhất
            Console.WriteLine("--> Đang khởi tạo cấu trúc Database (Migration)...");
            await context.Database.MigrateAsync();

            // 3. Nạp dữ liệu giả từ file SeedData.cs
            Console.WriteLine("--> Đang nạp dữ liệu Seed...");
            await SeedData.SeedDatabaseAsync(context, userManager);

            Console.WriteLine("==> SEED DATA THÀNH CÔNG! HỆ THỐNG ĐÃ SẴN SÀNG.");

            // Sau khi seed xong thường chúng ta sẽ dừng app để bạn chạy lại bình thường
            return;
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Lỗi nghiêm trọng khi đang Seed Data.");
            return;
        }
    }
}
// ================= KẾT THÚC ĐOẠN SEED DATA =================

app.Run();
