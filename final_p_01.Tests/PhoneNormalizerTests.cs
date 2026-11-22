using final_p_01.Application.Services;

namespace final_p_01.Tests;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("09123456789")]
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹")]
    [InlineData("٠٩١٢٣٤٥٦٧٨٩")]
    [InlineData("+98 912 345 6789")]
    [InlineData("0098-912-345-6789")]
    [InlineData("989123456789")]
    [InlineData("9123456789")]
    [InlineData("  09123456789  ")]
    public void ValidIranianMobileFormats_NormalizeTo09(string input)
    {
        Assert.Equal("09123456789", PhoneNormalizer.NormalizeTo09(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0912345678")]      // 10 digits with leading 0
    [InlineData("091234567890")]    // too long
    [InlineData("08123456789")]     // wrong prefix
    [InlineData("abcdefghijk")]
    public void InvalidInputs_ReturnNull(string? input)
    {
        Assert.Null(PhoneNormalizer.NormalizeTo09(input));
    }

    [Fact]
    public void ToLatinDigits_ConvertsPersianAndArabicDigits()
    {
        Assert.Equal("0123456789", PhoneNormalizer.ToLatinDigits("۰۱۲۳۴۵۶۷۸۹"));
        Assert.Equal("0123456789", PhoneNormalizer.ToLatinDigits("٠١٢٣٤٥٦٧٨٩"));
        Assert.Equal("abc0", PhoneNormalizer.ToLatinDigits("abc۰"));
    }
}
