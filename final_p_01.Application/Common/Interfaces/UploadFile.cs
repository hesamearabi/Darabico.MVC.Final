namespace final_p_01.Application.Common.Interfaces;

public sealed record UploadFile(string FileName, long Length, Func<Stream> OpenReadStream)
{
    public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        await using var source = OpenReadStream();
        await source.CopyToAsync(destination, cancellationToken);
    }
}
