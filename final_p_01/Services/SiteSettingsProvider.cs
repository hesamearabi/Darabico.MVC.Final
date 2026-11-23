using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace final_p_01.Services;

public interface ISiteSettingsProvider
{
    SiteSettingsSnapshot Get();
}

/// <summary>تنظیمات سراسری با کش کوتاه برای استفاده در لایهوت و صفحات عمومی.</summary>
public class SiteSettingsProvider : ISiteSettingsProvider
{
    private const int CacheTtlSeconds = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public SiteSettingsProvider(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public SiteSettingsSnapshot Get()
    {
        return _cache.GetOrCreateAsync("SiteSettingsProvider:Snapshot", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
                var s = await db.SiteSettings.AsNoTracking()
                    .AsNoTracking()
                    .Where(x => x.Id == Domain.Entities.SiteSettings.SingletonId)
                    .Select(x => new
                    {
                        x.EnableMultiLanguage,
                        x.SeoHomeTitle,
                        x.SeoHomeDescription,
                        x.SeoProjectsTitle,
                        x.SeoProjectsDescription,
                        x.SeoBlogTitle,
                        x.SeoBlogDescription,
                        x.SeoEstimatorTitle,
                        x.SeoEstimatorDescription,
                        x.SeoInquiryTitle,
                        x.SeoInquiryDescription,
                        x.SeoGuideTitle,
                        x.SeoGuideDescription,
                        x.FaviconPath,
                        x.UpdatedAtUtc
                    })
                    .FirstOrDefaultAsync();
                string? cacheBust = s?.UpdatedAtUtc.Ticks.ToString();

                return new SiteSettingsSnapshot
                {
                    EnableMultiLanguage = s?.EnableMultiLanguage ?? false,
                    SeoHomeTitle = s?.SeoHomeTitle,
                    SeoHomeDescription = s?.SeoHomeDescription,
                    SeoProjectsTitle = s?.SeoProjectsTitle,
                    SeoProjectsDescription = s?.SeoProjectsDescription,
                    SeoBlogTitle = s?.SeoBlogTitle,
                    SeoBlogDescription = s?.SeoBlogDescription,
                    SeoEstimatorTitle = s?.SeoEstimatorTitle,
                    SeoEstimatorDescription = s?.SeoEstimatorDescription,
                    SeoInquiryTitle = s?.SeoInquiryTitle,
                    SeoInquiryDescription = s?.SeoInquiryDescription,
                    SeoGuideTitle = s?.SeoGuideTitle,
                    SeoGuideDescription = s?.SeoGuideDescription,
                    FaviconPath = s?.FaviconPath,
                    CacheBust = cacheBust
                };
            }
            catch
            {
                return new SiteSettingsSnapshot();
            }
        }).GetAwaiter().GetResult();
    }
}

public class SiteSettingsSnapshot
{
    public bool EnableMultiLanguage { get; set; }
    public string? SeoHomeTitle { get; set; }
    public string? SeoHomeDescription { get; set; }
    public string? SeoProjectsTitle { get; set; }
    public string? SeoProjectsDescription { get; set; }
    public string? SeoBlogTitle { get; set; }
    public string? SeoBlogDescription { get; set; }
    public string? SeoEstimatorTitle { get; set; }
    public string? SeoEstimatorDescription { get; set; }
    public string? SeoInquiryTitle { get; set; }
    public string? SeoInquiryDescription { get; set; }
    public string? SeoGuideTitle { get; set; }
    public string? SeoGuideDescription { get; set; }
    public string? FaviconPath { get; set; }
    public string? CacheBust { get; set; }

    public (string? Title, string? Description) ForPage(string pageKey) => pageKey switch
    {
        "home" => (SeoHomeTitle, SeoHomeDescription),
        "projects" => (SeoProjectsTitle, SeoProjectsDescription),
        "blog" => (SeoBlogTitle, SeoBlogDescription),
        "estimator" => (SeoEstimatorTitle, SeoEstimatorDescription),
        "inquiry" => (SeoInquiryTitle, SeoInquiryDescription),
        "guide" => (SeoGuideTitle, SeoGuideDescription),
        _ => (null, null)
    };
}
