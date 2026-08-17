using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// تنظیمات هاستینگ
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🚀 پرفورمنس: فعال‌سازی فشرده‌سازی برای لود سریع‌تر صفحات سایت (سئو فنی عالی)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// تنظیمات سئو برای URLها
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

builder.Services.AddControllersWithViews();

// 🚀 پرفورمنس: فعال‌سازی سرویس کش خروجی برای کنترلرها (ارتباط با HomeController)
builder.Services.AddOutputCache();

// تنظیمات دیتابیس
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🚀 امنیت: پیکربندی پیشرفته و ضد-هک برای کوکی احراز هویت
builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        // جلوگیری از دسترسی کدهای جاوااسکریپت مخرب به کوکی (XSS)
        options.Cookie.HttpOnly = true;

        // کوکی فقط روی پروتکل امن HTTPS ارسال می‌شود
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // جلوگیری از ارسال فرم از دامنه‌های دیگر (CSRF)
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

app.UseForwardedHeaders();

// 🚀 امنیت (رفع باگ بحرانی Information Disclosure)
if (app.Environment.IsDevelopment())
{
    // این صفحه فقط در سیستم برنامه‌نویس باز می‌شود!
    app.UseDeveloperExceptionPage();
}
else
{
    // کاربران و هکرها فقط صفحه خطای معمولی را می‌بینند
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🚀 پرفورمنس: استفاده از فشرده‌سازی در خروجی‌ها
app.UseResponseCompression();

// 🚀 پرفورمنس: کش کردن فایل‌های استاتیک در مرورگر کاربر (افزایش شدید امتیاز Google PageSpeed)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // نگهداری عکس‌ها و استایل‌ها در مرورگر کاربر برای 30 روز
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=2592000";
    }
});

app.UseRouting();

// 🚀 پرفورمنس: فعال‌سازی میدل‌ور کش برای صفحات استاتیک
app.UseOutputCache();

// فعال‌سازی نگهبان‌ها (ترتیب دقیقاً باید همین باشد)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();