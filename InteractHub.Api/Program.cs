using System.Text;
using InteractHub.Api.Data;
using InteractHub.Api.Entities;
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
});

// Thêm dịch vụ Controllers
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Bỏ dòng app.MapOpenApi(); đi và thay bằng 2 dòng này:
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Kích hoạt Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map các đường dẫn API tới các Controller
app.MapControllers();

app.Run();
