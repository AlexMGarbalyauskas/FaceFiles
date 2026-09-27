namespace FaceFiles.Pages.People;

public static class PhotoStorage
{
    public static async Task<string?> SavePhotoAsync(IWebHostEnvironment environment, IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            return null;
        }

        var photoFolder = Path.Combine(environment.WebRootPath, "images", "user_photos");
        Directory.CreateDirectory(photoFolder);

        var extension = Path.GetExtension(photo.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(photoFolder, fileName);

        await using var stream = File.Create(fullPath);
        await photo.CopyToAsync(stream);

        return $"/images/user_photos/{fileName}";
    }

    public static void DeletePhoto(IWebHostEnvironment environment, string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return;
        }

        var relativePath = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
