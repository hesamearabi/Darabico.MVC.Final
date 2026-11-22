using System.Globalization;

namespace final_p_01.Application.Services;

/// <summary>
/// نرمال‌سازی شماره موبایل ایرانی با تبدیل ارقام فارسی/عربی به لاتین
/// و افزودن پیش‌شماره 09xxxxxxxxx.
/// </summary>
public static class PhoneNormalizer
{
    /// <summary>خروجی معتبر: 09xxxxxxxxx؛ نامعتبر: null</summary>
    public static string? NormalizeTo09(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = ToLatinDigits(input.Trim());

        // حذف جداکننده‌ها و کاراکترهای تزئینی
        digits = new string(digits.Where(char.IsAsciiDigit).ToArray());

        if (digits.StartsWith("0098"))
            digits = digits[4..];
        else if (digits.StartsWith("98") && digits.Length >= 12)
            digits = digits[2..];

        if (digits.StartsWith('9') && digits.Length == 10)
            digits = '0' + digits;

        if (digits.Length != 11 || !digits.StartsWith("09") || !digits.All(char.IsAsciiDigit))
            return null;

        return digits;
    }

    public static string ToLatinDigits(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is >= '\u06F0' and <= '\u06F9')          // ارقام فارسی
                sb.Append((char)(ch - '\u06F0' + '0'));
            else if (ch is >= '\u0660' and <= '\u0669')     // ارقام عربی
                sb.Append((char)(ch - '\u0660' + '0'));
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }
}
