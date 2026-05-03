public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public IFormFile? AvatarFile { get; set; } // Nhận file từ FE
}