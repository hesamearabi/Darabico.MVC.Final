using final_p_01.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace final_p_01.Infrastructure.Services;

public class ImageService(IWebHostEnvironment environment) : IImageService
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp"
    };

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm"
    };

    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const long MaxVideoSizeBytes = 80 * 1024 * 1024; // 80 MB

    // ابعاد ثابت کارت‌ها برای حذف کامل CLS (نسبت 3:2)
    private const int ThumbnailWidth = 600;
    private const int ThumbnailHeight = 400;

    public async Task<ProcessedImageResult> SaveProjectImageAsync(
        UploadFile file,
        string subFolder,
        bool generateThumbnail = false,
        int maxWidth = 1200,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("فایل تصویر ارسال نشده است.");

        if (file.Length > MaxImageSizeBytes)
            throw new InvalidOperationException("حجم تصویر نباید بیشتر از ۱۰ مگابایت باشد.");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException("فرمت تصویر مجاز نیست. فرمت‌های مجاز: JPG, PNG, WebP.");

        var targetDir = EnsureUploadDirectory(subFolder);
        var uniqueId = Guid.NewGuid().ToString("N");
        var webpFileName = $"{uniqueId}.webp";
        var webpFullPath = Path.Combine(targetDir, webpFileName);

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream, cancellationToken);

        image.Mutate(ctx =>
        {
            ctx.AutoOrient();
            if (image.Width > maxWidth)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, 0),
                    Mode = ResizeMode.Max
                });
            }
        });

        var encoder = new WebpEncoder { Quality = 82 };
        await image.SaveAsWebpAsync(webpFullPath, encoder, cancellationToken);

        int finalWidth = image.Width;
        int finalHeight = image.Height;
        string mainUrl = $"/uploads/{subFolder}/{webpFileName}";
        string? thumbUrl = null;

        if (generateThumbnail)
        {
            var thumbFileName = $"{uniqueId}_thumb.webp";
            var thumbFullPath = Path.Combine(targetDir, thumbFileName);

            using var thumbImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailWidth, ThumbnailHeight),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            await thumbImage.SaveAsWebpAsync(thumbFullPath, new WebpEncoder { Quality = 78 }, cancellationToken);
            thumbUrl = $"/uploads/{subFolder}/{thumbFileName}";
        }

        return new ProcessedImageResult(mainUrl, thumbUrl, finalWidth, finalHeight);
    }

    public async Task<string> SaveVideoAsync(
        UploadFile file,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new InvalidOperationException("فایل ویدیو ارسال نشده است.");

        if (file.Length > MaxVideoSizeBytes)
            throw new InvalidOperationException("حجم ویدیو نباید بیشتر از ۸۰ مگابایت باشد.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(ext))
            throw new InvalidOperationException("فقط فرمت‌های MP4 و WebM مجاز هستند.");

        await ValidateVideoMagicBytesAsync(file, ext, cancellationToken);

        var targetDir = EnsureUploadDirectory(subFolder);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{subFolder}/{fileName}";
    }

    public void DeleteFile(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var normalizedPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, normalizedPath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));

        // جلوگیری از Path Traversal
        if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string EnsureUploadDirectory(string subFolder)
    {
        var safeSubFolder = subFolder.Trim('/').Replace("..", string.Empty);
        var fullDir = Path.Combine(environment.WebRootPath, "uploads", safeSubFolder);
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        return fullDir;
    }

    // اعتبارسنجی بایت‌های ابتدایی فایل (magic bytes) تا محتوای جعلی با پسوند ویدیو ذخیره نشود
    private static async Task ValidateVideoMagicBytesAsync(
        UploadFile file, string extension, CancellationToken cancellationToken)
    {
        var buffer = new byte[12];
        var stream = file.OpenReadStream();
        int read = 0;
        while (read < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
            if (n == 0) break;
            read += n;
        }

        if (stream.CanSeek) stream.Position = 0;
        if (extension == ".webm")
        {
            // EBML header: 0x1A 0x45 0xDF 0xA3
            bool isEbml = read >= 4 &&
                buffer[0] == 0x1A && buffer[1] == 0x45 && buffer[2] == 0xDF && buffer[3] == 0xA3;
            if (!isEbml)
                throw new InvalidOperationException("محتوای فایل با فرمت WebM مطابقت ندارد.");
            return;
        }

        // MP4: bytes 4..7 are "ftyp"
        bool isMp4 = read >= 8 &&
            buffer[4] == (byte)'f' && buffer[5] == (byte)'t' &&
            buffer[6] == (byte)'y' && buffer[7] == (byte)'p';
        if (!isMp4)
            throw new InvalidOperationException("محتوای فایل با فرمت MP4 مطابقت ندارد.");
    }
}