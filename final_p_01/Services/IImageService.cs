namespace Darabico.Web.Services;

public record ProcessedImageResult(
    string ImageUrl,
    string? ThumbnailUrl,
    int Width,
    int Height);

public interface IImageService
{
    Task<ProcessedImageResult> SaveProjectImageAsync(
        IFormFile file,
        string subFolder,
        bool generateThumbnail = false,
        int maxWidth = 1200,
        CancellationToken cancellationToken = default);

    Task<string> SaveVideoAsync(
        IFormFile file,
        string subFolder,
        CancellationToken cancellationToken = default);

    void DeleteFile(string? relativeUrl);
}