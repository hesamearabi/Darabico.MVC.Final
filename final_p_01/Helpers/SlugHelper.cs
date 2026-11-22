using System.Text.RegularExpressions;

namespace Darabico.Web.Helpers;

public static partial class SlugHelper
{
    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant()
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Replace('\u200C', '-'); // تبدیل نیم‌فاصله به خط تیره

        // حفظ حروف فارسی، انگلیسی، اعداد و خط تیره
        normalized = InvalidCharsRegex().Replace(normalized, "-");
        normalized = MultipleHyphensRegex().Replace(normalized, "-").Trim('-');

        return normalized;
    }

    [GeneratedRegex(@"[^a-z0-9\u0600-\u06FF\-]+")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();
}