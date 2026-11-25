using final_p_01.Infrastructure;
using final_p_01.Areas.Admin.Controllers;
using final_p_01.Domain.Entities;
using final_p_01.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

builder.Services.AddSingleton<ISiteUrlBuilder, SiteUrlBuilder>();
builder.Services.AddSingleton<final_p_01.Services.IRichTextSanitizer, final_p_01.Services.RichTextSanitizer>();
builder.Services.AddScoped<final_p_01.Services.IMediaUploadService, final_p_01.Services.MediaUploadService>();
builder.Services.AddSingleton<ISiteSettingsProvider, SiteSettingsProvider>();
builder.Services.AddFileStorage();

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

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
        options.HttpsPort = 443;
    });
}

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
    options.AddBasePolicy(policyBuilder => policyBuilder.NoCache());
});

builder.Services.AddMemoryCache();
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Administrator")));

var loginRateLimitPermit = builder.Configuration.GetValue<int?>("AdminAuth:RateLimitPermitLimit") ?? 5;
var loginRateLimitWindowMinutes = builder.Configuration.GetValue<double?>("AdminAuth:RateLimitWindowMinutes") ?? 10;

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

    options.AddPolicy("AdminLoginRateLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = loginRateLimitPermit,
            Window = TimeSpan.FromMinutes(loginRateLimitWindowMinutes),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // محدودیت نرخ عمومی برای همه فرم‌های POST عمومی (لید، رزرو، پس از فروش)
    options.AddPolicy("FormRateLimit", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminFeedbackFilter>();
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"مقدار واردشده برای «{field}» معتبر نیست.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => "مقدار واردشده معتبر نیست.");
    messages.SetValueMustBeANumberAccessor(field => $"برای «{field}» یک عدد معتبر وارد کنید.");
    messages.SetValueMustNotBeNullAccessor(value => "تکمیل این مقدار الزامی است.");
    messages.SetMissingBindRequiredValueAccessor(field => $"تکمیل «{field}» الزامی است.");
    messages.SetValueIsInvalidAccessor(value => "مقدار واردشده معتبر نیست.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"مقدار «{field}» معتبر نیست.");
    options.ModelMetadataDetailsProviders.Add(new final_p_01.Models.PersianValidationMetadataProvider());
}).AddCookieTempDataProvider(options =>
{
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<final_p_01.Application.Projects.Queries.ListProjectsHandler>();
builder.Services.AddScoped<final_p_01.Application.Projects.Queries.GetProjectHandler>();

var adminSessionTimeoutHours = builder.Configuration.GetValue<double?>("AdminAuth:SessionTimeoutHours") ?? 12;

builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(adminSessionTimeoutHours);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                if (context.Principal?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var email = context.Principal.FindFirstValue(ClaimTypes.Name);
                var fingerprintClaim = context.Principal.FindFirstValue(AuthController.PasswordFingerprintClaimType);

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fingerprintClaim))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("DarabicoAuth");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var admin = await dbContext.AdminUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == email, context.HttpContext.RequestAborted);

                if (admin == null || string.IsNullOrEmpty(admin.Password))
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("DarabicoAuth");
                    logger.LogWarning(
                        "Admin session rejected because account {Email} no longer exists or has no password.",
                        AuthController.SanitizeForLog(email));

                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("DarabicoAuth");
                    return;
                }

                var expectedFingerprint = AuthController.ComputePasswordFingerprint(admin.Password);
                var claimBytes = Encoding.UTF8.GetBytes(fingerprintClaim);
                var expectedBytes = Encoding.UTF8.GetBytes(expectedFingerprint);

                if (claimBytes.Length != expectedBytes.Length ||
                    !CryptographicOperations.FixedTimeEquals(claimBytes, expectedBytes))
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("DarabicoAuth");
                    logger.LogWarning(
                        "Admin session rejected for {Email} because the password-hash fingerprint changed.",
                        AuthController.SanitizeForLog(email));

                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("DarabicoAuth");
                }
            }
        };
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

        if (!context.Posts.Any(p => p.Slug == "padel-court-construction-guide"))
        {
            var guideCategory = context.Categories.FirstOrDefault(c => c.Slug == "guides");
            if (guideCategory == null)
            {
                guideCategory = new Category { Name = "راهنماها", Slug = "guides" };
                context.Categories.Add(guideCategory);
                context.SaveChanges();
            }

            context.Posts.Add(new Post
            {
                Title = "راهنمای جامع 0 تا 100 ساخت زمین پدل",
                Slug = "padel-court-construction-guide",
                CategoryId = guideCategory.Id,
                Content = "<p>راهنمای گام‌به‌گام طراحی و اجرای زمین پدل: از انتخاب زمین و پی تا دیواره‌های شیشه‌ای، نورپردازی و تحویل نهایی. فصل‌های تخصصی این راهنما در همین صفحه در دسترس شماست.</p>",
                MetaTitle = "راهنمای جامع 0 تا 100 ساخت زمین پدل | دارابیکو",
                MetaDescription = "همه مراحل ساخت زمین پدل از صفر تا صد: انتخاب زمین، پی و آسفالت، دیواره‌ها، ابعاد استاندارد و تجهیزات FIP.",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        // صفحه پیلار «راهنمای جامع» منبع حقیقت مستقل ندارد؛ محتوای زنده از جدول Posts رندر می‌شود.
        // ایندکس‌گذاری مجدد PillarPage روی Post واقعی انجام می‌شود تا ادمین همان محتوای زنده را ببیند (بدون بازنویسی محتوا).
        var guidePostEntity = context.Posts.AsNoTracking().FirstOrDefault(p => p.Slug == "padel-court-construction-guide");
        var existingGuidePillar = context.PillarPages.FirstOrDefault(p => p.Slug == "padel-court-construction-guide");
        if (guidePostEntity != null && existingGuidePillar != null)
        {
            existingGuidePillar.IsPublished = false; // رندر عمومی از HomeController.PadelGuide (جدول Posts) انجام می‌شود
            context.SaveChanges();
        }
        else if (existingGuidePillar == null && guidePostEntity == null)
        {
            context.PillarPages.Add(new PillarPage
            {
                Title = "راهنمای جامع 0 تا 100 ساخت زمین پدل",
                Slug = "padel-court-construction-guide",
                ContentHtml = "<p>راهنمای گام‌به‌گام طراحی و اجرای زمین پدل: از انتخاب زمین و پی تا دیواره‌های شیشه‌ای، نورپردازی و تحویل نهایی. فصل‌های تخصصی این راهنما در همین صفحه در دسترس شماست.</p>",
                MetaTitle = "راهنمای جامع 0 تا 100 ساخت زمین پدل | دارابیکو",
                MetaDescription = "همه مراحل ساخت زمین پدل از صفر تا صد: انتخاب زمین، پی و آسفالت، دیواره‌ها، ابعاد استاندارد و تجهیزات FIP.",
                IsPublished = true,
                SortOrder = 0
            });
            context.SaveChanges();
        }

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

var canonicalBaseUrl = builder.Configuration["SiteSettings:BaseUrl"]?.Trim().TrimEnd('/') ?? "https://darabico.com";
var canonicalUri = Uri.TryCreate(canonicalBaseUrl, UriKind.Absolute, out var parsedCanonicalUri)
    ? parsedCanonicalUri
    : new Uri("https://darabico.com");
var canonicalHost = canonicalUri.Host;
var wwwCanonicalHost = $"www.{canonicalHost}";

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Head)
    {
        var originalPath = context.Request.Path.Value ?? string.Empty;

        if (originalPath.Equals("/home/error", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/home/pagenotfound", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var currentHost = context.Request.Host.Host;
        bool isWwwHost = !app.Environment.IsDevelopment() &&
                         currentHost.Equals(wwwCanonicalHost, StringComparison.OrdinalIgnoreCase);
        bool isHttpOnCanonicalHost = !app.Environment.IsDevelopment() &&
                                     !context.Request.IsHttps &&
                                     (currentHost.Equals(canonicalHost, StringComparison.OrdinalIgnoreCase) || isWwwHost);

        bool isExcludedPath =
            originalPath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/css-source", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/js-source", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/fonts", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/h-img", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/footerimg", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/sitelogo", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/sliderimg", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);

        bool isLegacyHome =
            originalPath.Equals("/home", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/home/index", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/home/", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/home/index/", StringComparison.OrdinalIgnoreCase);

        bool isLegacyHomeSubRoute = !isLegacyHome &&
                                    originalPath.StartsWith("/home/", StringComparison.OrdinalIgnoreCase);

        bool hasUpperCase = !isExcludedPath && !isLegacyHomeSubRoute && originalPath.Any(char.IsUpper);
        bool hasTrailingSlash = !isExcludedPath && !isLegacyHomeSubRoute && originalPath.Length > 1 && originalPath.EndsWith('/');
        bool needsPathRedirect = isLegacyHome || hasUpperCase || hasTrailingSlash;

        if (isHttpOnCanonicalHost || isWwwHost || needsPathRedirect)
        {
            string normalizedPath;

            if (isLegacyHome)
            {
                normalizedPath = "/";
            }
            else if (needsPathRedirect)
            {
                normalizedPath = hasUpperCase ? originalPath.ToLowerInvariant() : originalPath;
                if (normalizedPath.Length > 1 && normalizedPath.EndsWith('/'))
                {
                    normalizedPath = normalizedPath.TrimEnd('/');
                    if (string.IsNullOrEmpty(normalizedPath))
                    {
                        normalizedPath = "/";
                    }
                }
            }
            else
            {
                normalizedPath = string.IsNullOrEmpty(originalPath) ? "/" : originalPath;
            }

            string encodedPath = new PathString(normalizedPath).ToUriComponent();
            string queryString = context.Request.QueryString.Value ?? string.Empty;

            string destination = (isHttpOnCanonicalHost || isWwwHost)
                ? $"https://{canonicalHost}{encodedPath}{queryString}"
                : $"{encodedPath}{queryString}";

            context.Response.Redirect(destination, permanent: true);
            return;
        }
    }

    await next();
});

app.UseHttpsRedirection();

const string contentSecurityPolicyReportOnly =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline'; " +
    "style-src 'self' 'unsafe-inline'; " +
    "img-src 'self' data:; " +
    "font-src 'self' data:; " +
    "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com https://www.aparat.com; " +
    "media-src 'self'; " +
    "connect-src 'self'; " +
    "object-src 'none'; " +
    "base-uri 'self'; " +
    "form-action 'self'; " +
    "frame-ancestors 'self';";

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy-Report-Only"] = contentSecurityPolicyReportOnly;

    if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
    {
        headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
    }

    await next();
});

app.UseStatusCodePagesWithReExecute("/home/pagenotfound", "?statusCode={0}");

app.UseResponseCompression();

final_p_01.Extensions.StaticAssetExtensions.UseLegacyImagePaths(app, app.Environment);
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

        if (ctx.Context.Request.Path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase))
        {
            headers["Cache-Control"] = "public,max-age=3600";
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
        private readonly Uri _baseUri;

        public SiteUrlBuilder(IConfiguration configuration)
        {
            _baseUrl = configuration.GetValue<string>("SiteSettings:BaseUrl")?.Trim().TrimEnd('/') ?? "https://darabico.com";
            _baseUri = new Uri($"{_baseUrl}/");
        }

        public string Absolute(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return _baseUri.AbsoluteUri;
            }

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.TryCreate(path, UriKind.Absolute, out var externalUri)
                    ? externalUri.AbsoluteUri
                    : path;
            }

            string relativePath = "/" + path.TrimStart('/');
            return new Uri(_baseUri, relativePath).AbsoluteUri;
        }
    }
}