using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity; // اضافه شده برای PasswordHasher

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

// تنظیمات سئو برای URLها
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

// 🚀 تنظیمات سئو و پرفورمنس
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// 🚀 امنیت (HSTS)
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// 🚀 پرفورمنس: سیاست کش خروجی
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b =>
        b.With(c => !c.HttpContext.User.Identity.IsAuthenticated)
         .Expire(TimeSpan.FromMinutes(60)));
});

builder.Services.AddControllersWithViews();

// تنظیمات دیتابیس
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("final_p_01.Infrastructure")
    ));

// تنظیمات احراز هویت
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

// ==========================================
// اجرای خودکار Migration و ساخت ادمین با هش معتبر
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // تولید هش استاندارد دات‌نت به صورت قطعی
        var hasher = new PasswordHasher<object>();
        var hashedPassword = hasher.HashPassword(null, "2081189526Hesam");

        // درج یا آپدیت مستقیم در دیتابیس (بدون نیاز به معرفی کلاس مدل)
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

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");
app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
    }
});

app.UseResponseCompression();
app.UseOutputCache();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();