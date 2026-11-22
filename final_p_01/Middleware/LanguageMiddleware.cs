using final_p_01.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace final_p_01.Middleware;

/// <summary>
/// میان‌افزار زیرساخت چندزبانه (R-21): وقتی در تنظیمات خاموش است هیچ کاری نمی‌کند.
/// وقتی روشن است، پیشوند /en و /ar را می‌پذیرد و Culture را تنظیم می‌کند؛
/// مسیر فارسی پیش‌فرض بدون پیشوند باقی می‌ماند.
/// </summary>
public class LanguageMiddleware
{
    private const int CacheTtlSeconds = 30;

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LanguageMiddleware> _logger;

    public LanguageMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory,
        IMemoryCache cache, ILogger<LanguageMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        bool multiLanguageEnabled = await _cache.GetOrCreateAsync(
            "LanguageMiddleware:EnableMultiLanguage",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
                    return await db.SiteSettings.AsNoTracking()
                        .Where(s => s.Id == Domain.Entities.SiteSettings.SingletonId)
                        .Select(s => s.EnableMultiLanguage)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex) when (ex is DbException or InvalidOperationException)
                {
                    // Fail open: اگر دیتابیس در دسترس نبود، چندزبانه خاموش فرض می‌شود تا کل سایت ۵۰۰ ندهد.
                    _logger.LogWarning(ex, "Could not read EnableMultiLanguage; defaulting to disabled for {Ttl}s.", CacheTtlSeconds);
                    return false;
                }
            });

        var prefix = LanguageRouteHelper.TryParsePrefix(path, multiLanguageEnabled);
        if (prefix is not null)
        {
            context.Request.Path = LanguageRouteHelper.StripPrefix(path);
            context.Items[LanguageRouteHelper.CultureKey] = prefix;
        }

        await _next(context);
    }
}
