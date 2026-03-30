using System.Text;
using System.Text.Json;
using InteractHub.Api.Data;
using InteractHub.Api.DTOs.Responses;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddControllers();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký HttpContextAccessor để lấy được Base URL (https://localhost:...)
builder.Services.AddHttpContextAccessor();

// Đăng ký dịch vụ FileService (Plug & Play)
builder.Services.AddScoped<IFileService, LocalFileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// THÊM DÒNG NÀY ĐỂ MỞ CỬA CHO PHÉP TRUY CẬP ẢNH TỪ THƯ MỤC wwwroot
app.UseStaticFiles();

// Kích hoạt Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map các đường dẫn API tới các Controller
app.MapControllers();

app.Run();
