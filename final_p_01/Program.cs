using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
using final_p_01.Domain.Entities;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600;
});

builder.Services.AddSingleton<final_p_01.Services.ISiteUrlBuilder, final_p_01.Services.SiteUrlBuilder>();

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

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

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

builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.With(c => c.HttpContext.User?.Identity?.IsAuthenticated != true));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("RatingRateLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
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
    var env = services.GetRequiredService<IWebHostEnvironment>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        if (!context.AdminUsers.Any())
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var seedEmail = configuration["ADMIN_SEED_EMAIL"];
            var seedPassword = configuration["ADMIN_SEED_PASSWORD"];

            if (!string.IsNullOrEmpty(seedEmail) && !string.IsNullOrEmpty(seedPassword))
            {
                var hasher = new PasswordHasher<AdminUser>();
                var adminUser = new AdminUser
                {
                    Email = seedEmail,
                    FullName = "مدیر دارابیکو"
                };
                adminUser.Password = hasher.HashPassword(adminUser, seedPassword);

                context.AdminUsers.Add(adminUser);
                context.SaveChanges();
            }
            else
            {
                logger.LogWarning("ADMIN_SEED_EMAIL or ADMIN_SEED_PASSWORD is not set. No admin user was created.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database error during startup.");
        if (!env.IsDevelopment())
        {
            throw;
        }
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

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Head)
    {
        var originalPath = context.Request.Path.Value ?? string.Empty;

        if (!originalPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/CSS-SOURCE", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/JS-SOURCE", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/fonts", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/H-IMG", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/footerimg", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.StartsWith("/siteLOGO", StringComparison.OrdinalIgnoreCase) &&
            !originalPath.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
        {
            var hasUpperCase = originalPath.Any(char.IsUpper);
            var hasTrailingSlash = originalPath.Length > 1 && originalPath.EndsWith('/');

            bool isLegacyHome = originalPath.Equals("/home", StringComparison.OrdinalIgnoreCase) ||
                                originalPath.Equals("/home/index", StringComparison.OrdinalIgnoreCase) ||
                                originalPath.Equals("/home/", StringComparison.OrdinalIgnoreCase) ||
                                originalPath.Equals("/home/index/", StringComparison.OrdinalIgnoreCase);

            if (isLegacyHome || hasUpperCase || hasTrailingSlash)
            {
                string targetPath = originalPath;

                if (isLegacyHome)
                {
                    targetPath = "/";
                }
                else
                {
                    if (hasUpperCase)
                    {
                        var decodedPath = Uri.UnescapeDataString(originalPath);
                        var lowerPath = decodedPath.ToLowerInvariant();
                        targetPath = string.Join("/", lowerPath.Split('/').Select(Uri.EscapeDataString));
                    }

                    if (targetPath.Length > 1 && targetPath.EndsWith('/'))
                    {
                        targetPath = targetPath.TrimEnd('/');
                    }
                }

                var queryString = context.Request.QueryString.Value;
                var newUrl = $"{targetPath}{queryString}";

                context.Response.Redirect(newUrl, permanent: true);
                return;
            }
        }
    }
    await next();
});

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");

app.UseResponseCompression();

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

namespace final_p_01.Services
{
    public interface ISiteUrlBuilder
    {
        string Absolute(string path);
    }

    public class SiteUrlBuilder : ISiteUrlBuilder
    {
        private readonly string _baseUrl;

        public SiteUrlBuilder(IConfiguration configuration)
        {
            _baseUrl = configuration.GetValue<string>("SiteSettings:BaseUrl")?.TrimEnd('/') ?? "https://darabico.com";
        }

        public string Absolute(string path)
        {
            if (string.IsNullOrEmpty(path)) return _baseUrl;
            return $"{_baseUrl}/{path.TrimStart('/')}";
        }
    }
}