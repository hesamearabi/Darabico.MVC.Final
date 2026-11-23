namespace final_p_01.Application.Common.Interfaces;

public record ProcessedImageResult(string ImageUrl, string? ThumbnailUrl, int Width, int Height);

public interface IImageService
{
    Task<ProcessedImageResult> SaveProjectImageAsync(UploadFile file, string subFolder,
        bool generateThumbnail = false, int maxWidth = 1200, CancellationToken cancellationToken = default);
    Task<string> SaveVideoAsync(UploadFile file, string subFolder, CancellationToken cancellationToken = default);
    void DeleteFile(string? relativeUrl);
}
