using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;
using final_p_01.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// تنظیمات هاستینگ و پروکسی‌ها (برای اجرا پشت IIS یا Nginx)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🚀 پرفورمنس: فعال‌سازی فشرده‌سازی برای لود سریع‌تر صفحات
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>(); // بروتلی برای مرورگرهای مدرن
});
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// 🚀 SEO: تنظیمات روتینگ با حروف کوچک و حذف اسلش اضافه
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

// 🚀 امنیت (HSTS): تنظیم Strict-Transport-Security برای Production
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddControllersWithViews();
builder.Services.AddOutputCache();

// 🚀 معماری و دیتابیس: رفع باگ Assembly Mismatch برای Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("final_p_01.Infrastructure") // معرفی صریح لایه زیرساخت
    ));

// 🚀 امنیت: پیکربندی پیشرفته و ضد-هک برای کوکی احراز هویت
builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        options.Cookie.HttpOnly = true; // جلوگیری از XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // فقط روی HTTPS
        options.Cookie.SameSite = SameSiteMode.Strict; // جلوگیری از CSRF
    });

var app = builder.Build();

// =======================================================
// 🚀 معماری (Admin Seed) با رویکرد Fail-Fast
// =======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // اجرای Seed فقط در صورتی که هیچ ادمینی وجود نداشته باشد
        if (!context.AdminUsers.Any())
        {
            var adminEmail = configuration["AdminSettings:DefaultEmail"];
            var adminPassword = configuration["AdminSettings:DefaultPassword"];

            // 🚀 Fail-Fast: برنامه بدون پیکربندی امنیتی نباید بالا بیاید!
            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                throw new Exception("CRITICAL ERROR: AdminSettings (DefaultEmail or DefaultPassword) are missing in Configuration or User Secrets. Application cannot start securely.");
            }

            var passwordHasher = new PasswordHasher<AdminUser>();
            var defaultAdmin = new AdminUser
            {
                Email = adminEmail
            };

            // هش کردن قطعی و امن پسورد
            defaultAdmin.Password = passwordHasher.HashPassword(defaultAdmin, adminPassword);

            context.AdminUsers.Add(defaultAdmin);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Application startup failed during database initialization.");
        throw; // توقف اپلیکیشن در محیط Production
    }
}
// =======================================================

app.UseForwardedHeaders();

// 🚀 مدیریت خطاها بر اساس محیط
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // فعال‌سازی HSTS
}

// 🚀 SEO: هدایت خطاهای 404 به صفحه اختصاصی برای جلوگیری از ایندکس صفحات شکسته
app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseResponseCompression();

// 🚀 پرفورمنس: کشینگ فایل‌های استاتیک
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000"); // 30 روز
    }
});

app.UseRouting();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();