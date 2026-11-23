using final_p_01.Application.Common.Interfaces;

namespace final_p_01.Extensions;

public static class UploadFileExtensions
{
    public static UploadFile ToUploadFile(this IFormFile file) => new(file.FileName, file.Length, file.OpenReadStream);
}
