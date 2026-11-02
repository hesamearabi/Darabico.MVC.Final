using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 🚀 رفع ارور حجم آپلود در IIS (تا 100 مگابایت)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

builder.Services.AddWebEncoders(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🚀 SEO: تنظیم مسیرها
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false; // برای جلوگیری از داپلیکیت کانتنت سئو
});

// 🚀 Performance: فشرده‌سازی محتوا با اولویت Brotli
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/manifest+json",
        "image/svg+xml",
        "text/css",
        "application/javascript",
        "application/json",
        "text/html"
    });
});
// تنظیم سطح فشرده‌سازی روی Optimal برای تعادل بین سرعت سرور و سایز فایل
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// 🚀 ثبت سیستم کش خروجی (کاملاً سازگار با تگ‌گذاری)
builder.Services.AddOutputCache(options =>
{
    // این پالیسی باعث می‌شود کش فقط برای کاربران لاگین‌نکرده (پابلیک) اعمال شود
    // و مدیر سایت همیشه تغییرات زنده را ببیند
    options.AddBasePolicy(builder =>
        builder.With(c => c.HttpContext.User?.Identity?.IsAuthenticated != true));
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("final_p_01.Infrastructure")
    ));

builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var hasher = new PasswordHasher<object>();
        var hashedPassword = hasher.HashPassword(null, "2081189526Hesam");

        context.Database.ExecuteSqlRaw(
            "IF NOT EXISTS (SELECT 1 FROM AdminUsers WHERE Email = 'admin@darabico.com') " +
            "BEGIN INSERT INTO AdminUsers (Email, Password, FullName) VALUES ('admin@darabico.com', {0}, N'مدیر دارابیکو') END " +
            "ELSE " +
            "BEGIN UPDATE AdminUsers SET Password = {0} WHERE Email = 'admin@darabico.com' END",
            hashedPassword
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "خطایی در دیتابیس رخ داد.");
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// 🚀 Security Headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");
app.UseHttpsRedirection();

// میدلور فشرده‌سازی باید قبل از StaticFiles و Routing باشد
app.UseResponseCompression();

// 🚀 کش کردن قدرتمند فایل‌های استاتیک در مرورگر کاربر (CSS, JS, Fonts, Images)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.Headers;

        if (app.Environment.IsDevelopment())
        {
            headers["Cache-Control"] = "no-cache";
            return;
        }

        var isVersioned = ctx.Context.Request.Query.ContainsKey("v");
        headers["Cache-Control"] = isVersioned
            ? "public,max-age=31536000,immutable"
            : "public,max-age=2592000";
    }
});

app.UseRouting();

// 🚀 ترتیب Authentication و Authorization کاملاً درست است
app.UseAuthentication();
app.UseAuthorization();

// 🚀 میدلور OutputCache باید دقیقاً اینجا (بعد از احراز هویت و قبل از مپ شدن کنترلرها) باشد
app.UseOutputCache();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();