using System.Security;

namespace final_p_01.Services;

public interface IMediaUploadService
{
    /// <summary>ذخیره فایل صوتی پادکست (MP3/OGG حداکثر ۲۰ مگابایت) با اعتبارسنجی سمت سرور.</summary>
    Task<string> SaveAudioAsync(IFormFile file);

    /// <summary>ذخیره فایل دانلود (PDF/ZIP حداکثر ۵۰ مگابایت) با اعتبارسنجی سمت سرور.</summary>
    Task<string> SaveAttachmentAsync(IFormFile file);

    /// <summary>حذف امن فایل داخل پوشه uploads (جلوگیری از Path Traversal).</summary>
    void DeleteSecurely(string? relativePath);
}

public class MediaUploadService(
    IConfiguration configuration,
    IWebHostEnvironment env,
    ILogger<MediaUploadService> logger) : IMediaUploadService
{
    public async Task<string> SaveAudioAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("فایلی ارسال نشده است.");

        int maxMb = configuration.GetValue<int>("SiteSettings:MaxAudioUploadSizeMB", 20);
        if (file.Length > maxMb * 1024L * 1024L)
            throw new InvalidOperationException($"حجم فایل صوتی نباید بیشتر از {maxMb} مگابایت باشد.");

        if (!file.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("نوع محتوای فایل معتبر نیست. لطفاً فقط فایل صوتی آپلود کنید.");

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".mp3" or ".ogg"))
            throw new InvalidOperationException("فرمت صوتی مجاز نیست (فقط MP3, OGG).");

        return await SaveAsync(file, extension, "audios");
    }

    public async Task<string> SaveAttachmentAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("فایلی ارسال نشده است.");

        int maxMb = configuration.GetValue<int>("SiteSettings:MaxAttachmentUploadSizeMB", 50);
        if (file.Length > maxMb * 1024L * 1024L)
            throw new InvalidOperationException($"حجم فایل ضمیمه نباید بیشتر از {maxMb} مگابایت باشد.");

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".zip"))
            throw new InvalidOperationException("فرمت فایل مجاز نیست (فقط PDF, ZIP).");

        if (extension == ".pdf" && file.Length >= 4 &&
            file.ContentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) is false &&
            file.ContentType.StartsWith("binary/", StringComparison.OrdinalIgnoreCase) is false)
            throw new InvalidOperationException("نوع محتوای فایل معتبر نیست.");

        return await SaveAsync(file, extension, "attachments");
    }

    private async Task<string> SaveAsync(IFormFile file, string extension, string subFolder)
    {
        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string uploadsFolder = Path.Combine(env.WebRootPath, "uploads", subFolder);

        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }

    public void DeleteSecurely(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        try
        {
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                            .Replace('\\', Path.DirectorySeparatorChar)
                                            .TrimStart(Path.DirectorySeparatorChar);

            string targetPath = Path.GetFullPath(Path.Combine(env.WebRootPath, normalized));
            string allowedDirectory = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;

            if (targetPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to securely delete old media file at path: {Path}", relativePath);
        }
    }
}
