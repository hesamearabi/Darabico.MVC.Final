namespace final_p_01.Application.Services;

public static class DigitNormalizer
{
    public static string Normalize(string? value) => new((value ?? "").Select(c =>
        c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') :
        c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c).ToArray());
}
