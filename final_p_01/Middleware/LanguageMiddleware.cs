using final_p_01.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Middleware;

/// <summary>
/// میان‌افزار زیرساخت چندزبانه (R-21): وقتی در تنظیمات خاموش است هیچ کاری نمی‌کند.
/// وقتی روشن است، پیشوند /en و /ar را می‌پذیرد و Culture را تنظیم می‌کند؛
/// مسیر فارسی پیش‌فرض بدون پیشوند باقی می‌ماند.
/// </summary>
public class LanguageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public LanguageMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        bool multiLanguageEnabled;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
            multiLanguageEnabled = await db.SiteSettings.AsNoTracking()
                .Where(s => s.Id == Domain.Entities.SiteSettings.SingletonId)
                .Select(s => s.EnableMultiLanguage)
                .FirstOrDefaultAsync();
        }

        var prefix = LanguageRouteHelper.TryParsePrefix(path, multiLanguageEnabled);
        if (prefix is not null)
        {
            context.Request.Path = LanguageRouteHelper.StripPrefix(path);
            context.Items[LanguageRouteHelper.CultureKey] = prefix;
        }

        await _next(context);
    }
}
