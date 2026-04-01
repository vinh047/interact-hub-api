namespace InteractHub.Api.Services;

public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile file, string folderName = "uploads");
}