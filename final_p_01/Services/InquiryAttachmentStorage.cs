using final_p_01.Domain.Entities;
using SixLabors.ImageSharp;

namespace final_p_01.Services;

public interface IInquiryAttachmentStorage
{
    Task<InquiryLeadAttachment> SaveAsync(IFormFile file, CancellationToken cancellationToken);
    Stream Open(string key);
    void Delete(string key);
}

public sealed class InquiryAttachmentStorage : IInquiryAttachmentStorage
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private readonly string root;
    public InquiryAttachmentStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["InquiryStorage:Root"];
        if (string.IsNullOrWhiteSpace(configured))
            configured = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "App_Data", "inquiry-attachments"));
        else if (!Path.IsPathFullyQualified(configured))
            throw new InvalidOperationException("Configure InquiryStorage:Root as an absolute durable private storage path.");
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configured));
        var webRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot")));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (root.Equals(webRoot, comparison) || root.StartsWith(webRoot + Path.DirectorySeparatorChar, comparison))
            throw new InvalidOperationException("Inquiry storage must be outside wwwroot.");
        Directory.CreateDirectory(root);
    }

    public async Task<InquiryLeadAttachment> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxBytes) throw new InvalidDataException("حجم هر فایل باید بین ۱ بایت و ۵ مگابایت باشد.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".pdf"))
            throw new InvalidDataException("فقط فایل JPG، PNG یا PDF مجاز است.");
        using var buffer = new MemoryStream();
        await using (var input = file.OpenReadStream())
        {
            var block = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(block, cancellationToken)) > 0)
            {
                if (buffer.Length + read > MaxBytes) throw new InvalidDataException("حجم فایل بیش از ۵ مگابایت است.");
                await buffer.WriteAsync(block.AsMemory(0, read), cancellationToken);
            }
        }
        var bytes = buffer.ToArray();
        string contentType;
        if (extension == ".pdf")
        {
            if (!bytes.AsSpan().StartsWith("%PDF-"u8) || !System.Text.Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024))).Contains("%%EOF"))
                throw new InvalidDataException("ساختار فایل PDF معتبر نیست.");
            contentType = "application/pdf";
        }
        else
        {
            try
            {
                var format = Image.DetectFormat(bytes);
                if ((extension == ".png" && format.Name != "PNG") || (extension != ".png" && format.Name != "JPEG"))
                    throw new InvalidDataException("محتوای فایل با پسوند آن مطابقت ندارد.");
                var info = Image.Identify(bytes);
                if ((long)info.Width * info.Height > 40_000_000) throw new InvalidDataException("ابعاد تصویر بیش از حد مجاز است.");
                using var decoded = Image.Load(bytes);
                contentType = extension == ".png" ? "image/png" : "image/jpeg";
            }
            catch (UnknownImageFormatException) { throw new InvalidDataException("تصویر معتبر نیست."); }
            catch (InvalidImageContentException) { throw new InvalidDataException("تصویر معتبر نیست."); }
        }
        var key = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(root, key);
        try { await File.WriteAllBytesAsync(path, bytes, cancellationToken); }
        catch { File.Delete(path); throw; }
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        return new InquiryLeadAttachment { StorageKey = key, OriginalFileName = name[..Math.Min(name.Length, 255)], ContentType = contentType, ByteLength = bytes.Length };
    }

    private string Resolve(string key)
    {
        if (key != Path.GetFileName(key) || key.Contains('\\') || key.Contains("..")) throw new InvalidDataException("Invalid storage key.");
        return Path.Combine(root, key);
    }
    public Stream Open(string key) => File.OpenRead(Resolve(key));
    public void Delete(string key) => File.Delete(Resolve(key));
}
