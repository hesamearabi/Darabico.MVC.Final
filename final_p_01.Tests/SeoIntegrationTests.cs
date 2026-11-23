using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Darabico.Domain.Entities;
using Darabico.Domain.Enums;
using Darabico.Web.Services;
using final_p_01.Controllers;
using final_p_01.Infrastructure.Data;
using final_p_01.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace final_p_01.Tests;

public sealed class SeoApplication : IAsyncLifetime
{
    public const string Origin = "https://seo-staging.example";
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"darabico-seo-{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(directory);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "final_p_01.sln"))) root = root.Parent;
        var contentRoot = Path.Combine(root!.FullName, "final_p_01");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HomeController).Assembly.GetName().Name,
            ContentRootPath = contentRoot,
            WebRootPath = Path.Combine(contentRoot, "wwwroot"),
            EnvironmentName = "Testing"
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SiteSettings:BaseUrl"] = Origin,
            ["InquiryStorage:Root"] = Path.Combine(directory, "attachments")
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(HomeController).Assembly);
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(directory, "seo.db")}"));
        builder.Services.AddSingleton<ISiteUrlBuilder, SiteUrlBuilder>();
        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services.AddScoped<IInquiryAttachmentStorage, InquiryAttachmentStorage>();
        builder.Services.AddAuthentication("test").AddCookie("test");
        builder.Services.AddAuthorization();
        builder.Services.AddOutputCache();
        App = builder.Build();
        App.UseDeveloperExceptionPage();
        App.UseStaticFiles();
        App.UseRouting();
        App.UseAuthentication();
        App.UseAuthorization();
        App.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
        App.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await using (var scope = App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            for (var number = 1; number <= 14; number++)
            {
                db.Projects.Add(new Project
                {
                    Title = number == 1 ? "پروژه <پدل> & تهران" : $"پروژه {number}",
                    Slug = $"project-{number}", City = "تهران", CourtCount = 2,
                    CourtType = CourtType.Outdoor, ExecutionYear = 1404, ExecutionDurationMonths = 3,
                    Description = "اجرای زمین پدل", IsPublished = number <= 13, DisplayOrder = number,
                    FeaturedImageUrl = "/H-IMG/aratehcourt.webp", ThumbnailUrl = "/H-IMG/aratehcourt.webp",
                    CreatedAtUtc = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = number == 1 ? new DateTime(2025, 6, 3, 0, 0, 0, DateTimeKind.Utc) : null
                });
            }
            await db.SaveChangesAsync();
        }
        await App.StartAsync();
        var address = App.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(directory, true);
    }
}

public sealed class SeoIntegrationTests(SeoApplication app) : IClassFixture<SeoApplication>
{
    private async Task<string> Get(string path)
    {
        using var response = await app.Client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {response.StatusCode}\n{body}");
        return body;
    }

    private static string[] Tags(string html, string pattern) => Regex.Matches(html, pattern, RegexOptions.IgnoreCase).Select(m => m.Value).ToArray();
    private static string Attribute(string tag, string name) => WebUtility.HtmlDecode(Regex.Match(tag, $"\\b{name}=\"([^\"]*)\"").Groups[1].Value);
    private static string Canonical(string html) => Attribute(Assert.Single(Tags(html, "<link\\b[^>]*rel=\"canonical\"[^>]*>")), "href");

    [Fact]
    public async Task SitemapIncludesOnlyPublishedProjectsWithRealDatesAndPublicEntryPoints()
    {
        var xml = XDocument.Parse(await Get("/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = xml.Root!.Elements(ns + "url").ToDictionary(e => e.Element(ns + "loc")!.Value);
        foreach (var path in new[] { "/projects", "/estimator", "/inquiry" }) Assert.Contains(SeoApplication.Origin + path, urls.Keys);
        for (var number = 1; number <= 13; number++) Assert.Contains($"{SeoApplication.Origin}/projects/project-{number}", urls.Keys);
        Assert.DoesNotContain(SeoApplication.Origin + "/projects/project-14", urls.Keys);
        Assert.DoesNotContain(SeoApplication.Origin + "/inquiry/thanks", urls.Keys);
        Assert.DoesNotContain(urls.Keys, url => new Uri(url).AbsolutePath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase));
        Assert.All(urls.Keys, url => Assert.StartsWith(SeoApplication.Origin + "/", url));
        Assert.Equal("2025-06-03", urls[SeoApplication.Origin + "/projects/project-1"].Element(ns + "lastmod")!.Value);
        Assert.Equal("2025-01-02", urls[SeoApplication.Origin + "/projects/project-2"].Element(ns + "lastmod")!.Value);
    }

    [Fact]
    public async Task DetailsHaveOneCanonicalDescriptionAndMatchingSafeBreadcrumbSchema()
    {
        var html = await Get("/projects/project-1");
        var canonical = Canonical(html);
        Assert.Equal(SeoApplication.Origin + "/projects/project-1", canonical);
        Assert.Single(Tags(html, "<meta\\b[^>]*name=\"description\"[^>]*>"));
        Assert.Equal(canonical, Attribute(Assert.Single(Tags(html, "<meta\\b[^>]*property=\"og:url\"[^>]*>")), "content"));
        var schemas = Regex.Matches(html, "(<script\\b[^>]*>)(.*?)</script>", RegexOptions.Singleline)
            .Where(m => Attribute(m.Groups[1].Value, "type") == "application/ld+json")
            .Select(m => JsonDocument.Parse(m.Groups[2].Value).RootElement.Clone()).ToArray();
        var breadcrumb = Assert.Single(schemas, s => s.TryGetProperty("@type", out var type) && type.GetString() == "BreadcrumbList");
        Assert.Equal("https://schema.org", breadcrumb.GetProperty("@context").GetString());
        var items = breadcrumb.GetProperty("itemListElement").EnumerateArray().ToArray();
        Assert.Equal(3, items.Length);
        var names = new[] { "خانه", "پروژه‌ها", "پروژه <پدل> & تهران" };
        var paths = new[] { "/", "/projects", "/projects/project-1" };
        for (var i = 0; i < items.Length; i++)
        {
            Assert.Equal("ListItem", items[i].GetProperty("@type").GetString());
            Assert.Equal(i + 1, items[i].GetProperty("position").GetInt32());
            Assert.Equal(names[i], items[i].GetProperty("name").GetString());
            Assert.Equal(SeoApplication.Origin + paths[i], items[i].GetProperty("item").GetString());
        }
    }

    [Fact]
    public async Task LegacyDetailRedirectsPermanentlyAndDraftsAreNotAccessible()
    {
        using var response = await app.Client.GetAsync("/projects/details/project-1");
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/projects/project-1", response.Headers.Location!.OriginalString);
        using var draft = await app.Client.GetAsync("/projects/project-14");
        Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
    }

    [Fact]
    public async Task PageTwoHasServerRenderedLinksAndSelfReferencingCanonical()
    {
        var html = await Get("/projects?page=2");
        Assert.Equal(SeoApplication.Origin + "/projects?page=2", Canonical(html));
        var links = Tags(html, "<a\\b[^>]*>").Select(tag => Attribute(tag, "href")).ToArray();
        foreach (var page in new[] { 1, 2, 3 }) Assert.Contains($"/projects?page={page}", links);
        Assert.Contains("صفحه قبل", WebUtility.HtmlDecode(html));
        Assert.Contains("صفحه بعد", WebUtility.HtmlDecode(html));
        Assert.Contains("data-page=\"2\"", html);
        Assert.Equal(6, Tags(html, "<article class=\"dc-projects-card\">").Length);
        Assert.DoesNotContain("href=\"/projects/project-1\"", html);
        Assert.Contains("href=\"/projects/project-7\"", html);
        var more = await Get("/projects/load-more?page=3");
        Assert.Single(Tags(more, "<article class=\"dc-projects-card\">"));
        Assert.Contains("/projects/project-13", more);
    }

    [Theory]
    [InlineData("city=%D8%AA%D9%87%D8%B1%D8%A7%D9%86")]
    [InlineData("courtType=Outdoor")]
    [InlineData("city=%D8%AA%D9%87%D8%B1%D8%A7%D9%86&courtType=Outdoor")]
    public async Task FilteredPaginationPreservesFiltersButCanonicalizesToArchive(string filters)
    {
        var html = await Get($"/projects?page=2&{filters}");
        Assert.Equal(SeoApplication.Origin + "/projects", Canonical(html));
        var next = Attribute(Assert.Single(Tags(html, "<a\\b[^>]*rel=\"next\"[^>]*>")), "href");
        Assert.Contains("page=3", next);
        foreach (var filter in filters.Split('&')) Assert.Contains(filter, next, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 1, 6)]
    [InlineData(-1, 1, 6)]
    [InlineData(1, 1, 6)]
    [InlineData(3, 3, 1)]
    [InlineData(int.MaxValue, 3, 1)]
    public async Task PaginationBoundsAreSafe(int requested, int actual, int count)
    {
        var html = await Get($"/projects?page={requested}");
        Assert.Equal(SeoApplication.Origin + (actual == 1 ? "/projects" : $"/projects?page={actual}"), Canonical(html));
        Assert.Equal(count, Tags(html, "<article class=\"dc-projects-card\">").Length);
    }

    [Fact]
    public async Task EmptyFilterHasNoPagination()
    {
        var html = await Get("/projects?city=missing&page=2");
        Assert.Equal(SeoApplication.Origin + "/projects", Canonical(html));
        Assert.Empty(Tags(html, "<article class=\"dc-projects-card\">"));
        Assert.DoesNotContain("dc-projects-page-links", html);
    }

    [Fact]
    public async Task EstimatorHasOneH1AndHomepageEmbeddedEstimatorKeepsH2AndFormContract()
    {
        var estimator = await Get("/estimator");
        Assert.Single(Tags(estimator, "<h1\\b[^>]*>"));
        Assert.Contains("<h1 id=\"estimator-title\">", estimator);
        Assert.Contains("تومان", WebUtility.HtmlDecode(estimator));
        Assert.Single(Tags(estimator, "<meta\\b[^>]*name=\"description\"[^>]*>"));
        var home = await Get("/");
        Assert.Contains("<h2 id=\"estimator-title\">", home);
        Assert.Single(Tags(home, "<h1\\b[^>]*>"));
        foreach (var html in new[] { estimator, home })
        {
            Assert.Contains("class=\"dc-estimator-form\" data-estimator", html);
            Assert.Contains("action=\"/estimator\"", html);
            Assert.Contains("name=\"__RequestVerificationToken\"", html);
        }
        Assert.Contains("مشاهده همه پروژه‌های اجرا شده", WebUtility.HtmlDecode(home));
        Assert.Equal(5, Tags(home, "<a class=\"gallery-item[^\"]*\" href=\"/projects\">").Length);
    }

    [Fact]
    public async Task ConfirmationIsNoindexButCrawlableAndRobotsUsesConfiguredOrigin()
    {
        var html = await Get("/inquiry/thanks");
        Assert.Equal("noindex, follow", Attribute(Assert.Single(Tags(html, "<meta\\b[^>]*name=\"robots\"[^>]*>")), "content"));
        var robots = await Get("/robots.txt");
        Assert.Contains($"Sitemap: {SeoApplication.Origin}/sitemap.xml", robots);
        Assert.Contains("Disallow: /admin/", robots);
        Assert.DoesNotContain("/inquiry", robots);
    }
}
