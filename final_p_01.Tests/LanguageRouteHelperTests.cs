using final_p_01.Application.Services;

namespace final_p_01.Tests;

public class LanguageRouteHelperTests
{
    [Theory]
    [InlineData("/en/projects", "en")]
    [InlineData("/AR/", "ar")]
    [InlineData("/ar/booking/3", "ar")]
    public void WhenEnabled_ParsesSupportedPrefix(string path, string expected)
    {
        Assert.Equal(expected, LanguageRouteHelper.TryParsePrefix(path, multiLanguageEnabled: true));
    }

    [Theory]
    [InlineData("/projects")]
    [InlineData("/fa/projects")]   // فارسی همیشه بدون پیشوند است
    [InlineData("/fr/projects")]   // زبان پشتیبانی‌نشده
    [InlineData("/")]
    [InlineData(null)]
    public void WhenEnabled_UnknownPrefixes_ReturnNull(string? path)
    {
        Assert.Null(LanguageRouteHelper.TryParsePrefix(path, multiLanguageEnabled: true));
    }

    [Theory]
    [InlineData("/en/projects")]
    [InlineData("/projects")]
    public void WhenDisabled_NoPrefixAccepted(string path)
    {
        Assert.Null(LanguageRouteHelper.TryParsePrefix(path, multiLanguageEnabled: false));
    }

    [Fact]
    public void StripPrefix_RemovesFirstSegment()
    {
        Assert.Equal("/projects", LanguageRouteHelper.StripPrefix("/en/projects"));
        Assert.Equal("/booking/3", LanguageRouteHelper.StripPrefix("/ar/booking/3"));
        Assert.Equal("/", LanguageRouteHelper.StripPrefix("/en"));
    }
}
