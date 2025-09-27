using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace YourApp.Shared.Utils;

public class CloudinaryUploader
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryUploader(IConfiguration configuration)
    {
        var account = new Account(
            Environment.GetEnvironmentVariable("CLOUD_STORAGE_PROVIDER") ?? configuration["Cloud:Provider"],
            Environment.GetEnvironmentVariable("CLOUD_STORAGE_API_KEY") ?? configuration["Cloud:APIKey"],
            Environment.GetEnvironmentVariable("CLOUD_STORAGE_API_SECRET") ?? configuration["Cloud:APISecret"]
        );
        _cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };
    }

    public async Task<(string? Url, string PublicId)> UploadImage(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            throw new InvalidOperationException("Invalid file type. Only images are allowed.");
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB limit
        {
            throw new InvalidOperationException("File size exceeds the 5MB limit.");
        }
        
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = true
        };
        
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        
        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }
        
        return (uploadResult.SecureUrl?.ToString(), uploadResult.PublicId);
    }
    
    public async Task<string> DeleteImage(string publicId)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result;
    }
}