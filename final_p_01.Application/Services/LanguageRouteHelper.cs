namespace final_p_01.Application.Services;

/// <summary>
/// کمکی پیشوند مسیر چندزبانه (/en، /ar). وقتی EnableMultiLanguage خاموش است،
/// هیچ پیشوندی پذیرفته نمی‌شود.
/// </summary>
public static class LanguageRouteHelper
{
    public const string Persian = "fa";
    public const string English = "en";
    public const string Arabic = "ar";
    public const string CultureKey = "Darabico.Culture";

    public static readonly string[] SupportedPrefixes = { English, Arabic };

    /// <summary>پیشوند زبان را از ابتدای path استخراج می‌کند؛ در حالت خاموش null برمی‌گرداند.</summary>
    public static string? TryParsePrefix(string? path, bool multiLanguageEnabled)
    {
        if (!multiLanguageEnabled || string.IsNullOrWhiteSpace(path))
            return null;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return null;

        var first = segments[0].ToLowerInvariant();
        return SupportedPrefixes.Contains(first) ? first : null;
    }

    /// <summary>مسیر بدون پیشوند زبان</summary>
    public static string StripPrefix(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length <= 1 ? "/" : "/" + string.Join('/', segments[1..]);
    }
}
