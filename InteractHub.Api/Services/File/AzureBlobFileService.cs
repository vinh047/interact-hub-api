using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InteractHub.Api.Services;

public class AzureBlobFileService : IFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureBlobFileService(IConfiguration configuration)
    {
        // Đọc cấu hình từ appsettings.json
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "media";
        
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName = "")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File cannot be empty.");

        // 1. Tạo container nếu nó chưa tồn tại (Và mở quyền Public Read)
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // 2. Tạo tên file độc nhất để không bị đè file (Ví dụ: posts/20260403_abc123.jpg)
        var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{folderName}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}{extension}";

        // Lấy client cho file cụ thể
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        // 3. Upload file lên mây
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        // 4. Trả về đường link URL tuyệt đối của ảnh trên Azure
        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        try
        {
            // Bóc tách tên file từ đường link URL
            var uri = new Uri(fileUrl);
            var blobName = uri.Segments[^2] + uri.Segments[^1]; // Lấy phần folder/filename ở cuối link

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Ra lệnh xóa
            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }
        catch
        {
            // Nếu có lỗi parse URL hoặc không tìm thấy, log lại hoặc bỏ qua
            return false;
        }
    }
}