namespace InteractHub.Api.Services;

public class LocalFileService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor) : IFileService
{
    public async Task<string> UploadFileAsync(IFormFile file, string folderName = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.");

        // 1. Xác định đường dẫn tới thư mục wwwroot/uploads
        var webRootPath = environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, folderName);

        // Nếu thư mục chưa tồn tại thì tạo mới
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 2. Đổi tên file để chống trùng lặp (Ví dụ: avatar.jpg -> 1234abcd-avatar.jpg)
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // 3. Copy file từ RAM xuống Ổ cứng
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // 4. Tạo URL để Frontend có thể lấy ảnh về hiển thị
        // Ví dụ: https://localhost:7000/uploads/1234abcd-avatar.jpg
        var request = httpContextAccessor.HttpContext!.Request;
        var baseUrl = $"{request.Scheme}://{request.Host.Value}";
        
        return $"{baseUrl}/{folderName}/{uniqueFileName}";
    }
}