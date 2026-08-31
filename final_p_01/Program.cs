using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// 🚀 Encoding: پشتیبانی کامل از کاراکترهای فارسی و جلوگیری از علامت 
builder.Services.AddWebEncoders(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// تنظیمات هاستینگ برای سرور
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// تنظیمات سئو برای URLها (حروف کوچک و حذف اسلش اضافه)
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

// 🚀 تنظیمات سئو و پرفورمنس: فشرده‌سازی پیشرفته با Brotli و Gzip
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// 🚀 امنیت (HSTS): تنظیم Strict-Transport-Security
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// 🚀 پرفورمنس: سیاست کش خروجی (استثنا برای ادمین)
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b =>
        b.With(c => !c.HttpContext.User.Identity.IsAuthenticated)
         .Expire(TimeSpan.FromMinutes(60)));
});

builder.Services.AddControllersWithViews();

// تنظیمات دیتابیس (همراه با رفع خطای Assembly)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("final_p_01.Infrastructure") // معرفی لایه زیرساخت
    ));

// تنظیمات احراز هویت (امنیت کوکی مخصوص دارابیکو)
builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        // 🚀 Security: محافظت پیشرفته از کوکی
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

// ==========================================
// اجرای خودکار Migration روی سرور (ساخت جداول)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // این خط تمام جداول را در اولین اجرا می‌سازد
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "خطایی هنگام اعمال Migration در دیتابیس رخ داد.");
    }
}

// تنظیمات Pipeline
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

// 🚀 SEO: هدایت خطاهای 404 به ویو اختصاصی
app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");

app.UseHttpsRedirection();

// 🚀 پرفورمنس: هدرهای کش برای تصاویر و CSS/JS
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000"); // 30 روز
    }
});

app.UseResponseCompression();
app.UseOutputCache();
app.UseRouting();

// فعال‌سازی نگهبان‌ها
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();