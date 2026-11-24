using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Darabico.Domain.Entities;
using Darabico.Domain.Enums;
using final_p_01.Application.Common.Interfaces;
using final_p_01.Infrastructure;
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
using final_p_01.Domain.Entities;

namespace final_p_01.Tests;

public sealed class SeoApplication : IAsyncLifetime
{
    public const string Origin = "https://seo-staging.example";
    public WebApplication App { get; private set; } = null!;
    public IServiceProvider Services => App.Services;
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
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<final_p_01.Services.ISiteSettingsProvider, final_p_01.Services.SiteSettingsProvider>();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(directory, "seo.db")}"));
        builder.Services.AddScoped<final_p_01.Application.Common.Interfaces.IAppDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<final_p_01.Application.Projects.Queries.ListProjectsHandler>();
        builder.Services.AddScoped<final_p_01.Application.Projects.Queries.GetProjectHandler>();
        builder.Services.AddSingleton<ISiteUrlBuilder, SiteUrlBuilder>();
        builder.Services.AddFileStorage();
        builder.Services.AddAuthentication("test").AddCookie("test");
        builder.Services.AddAuthorization();
        builder.Services.AddOutputCache();
        App = builder.Build();
        App.UseDeveloperExceptionPage();
        final_p_01.Extensions.StaticAssetExtensions.UseLegacyImagePaths(App, App.Environment);
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
            var settings = await db.SiteSettings.SingleAsync();
            settings.EnableMultiLanguage = true;
            var category = db.Categories.Add(new Category { Name = "راهنما", Slug = "guide" }).Entity;
            await db.SaveChangesAsync();
            db.Posts.Add(new Post
            {
                Title = "راهنمای جامع 0 تا 100 ساخت زمین پدل",
                Slug = "padel-court-construction-guide",
                Content = "<p>راهنمای کامل ساخت زمین پدل</p>",
                MetaDescription = "راهنمای جامع ساخت زمین پدل",
                IsPublished = true,
                CategoryId = category.Id
            });
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

    private async Task<(HttpStatusCode Status, string Body)> GetWithStatus(string path)
    {
        using var response = await app.Client.GetAsync(path);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
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
        Assert.Contains("<h2 id=\"estimator-title-home\">", home);
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

    [Theory]
    [InlineData("/")]
    [InlineData("/projects")]
    [InlineData("/estimator")]
    [InlineData("/inquiry")]
    public async Task RenderedAssetsUseStandardFoldersAndExist(string path)
    {
        var html = await Get(path);
        foreach (var tag in Tags(html, "<(?:link|script)\\b[^>]*(?:href|src)=\"[^\"]+\"[^>]*>"))
        {
            var attribute = tag.StartsWith("<script", StringComparison.OrdinalIgnoreCase) ? "src" : "href";
            var asset = Attribute(tag, attribute).Split('?')[0];
            if (!asset.EndsWith(".css") && !asset.EndsWith(".js")) continue;
            Assert.Matches("^/(css|js|lib)/", asset);
            Assert.True(File.Exists(Path.Combine(app.App.Environment.WebRootPath, asset.TrimStart('/'))), asset);
        }
        Assert.DoesNotContain("minislider.js", html);
        Assert.DoesNotContain("slider-body.js", html);
    }

    [Fact]
    public void EveryLocalViewAndStylesheetAssetReferenceExists()
    {
        var root = app.App.Environment.ContentRootPath;
        var webRoot = app.App.Environment.WebRootPath;
        var files = Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Areas"), "*.cshtml", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(webRoot, "*.css", SearchOption.AllDirectories));
        var references = 0;
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content,
                "[\"'(](?<path>(?:~?/)?(?:images|fonts|css|js|lib)/[^\"'<>\\r\\n]*?\\.(?:css|js|png|jpe?g|webp|svg|ico|woff2?|ttf|mp4|webm)(?:[?#][^\"'<>\\r\\n]*)?)[\"')]", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var asset = Uri.UnescapeDataString(match.Groups["path"].Value.Split('?', '#')[0]).TrimStart('~', '/');
                Assert.True(File.Exists(Path.Combine(webRoot, asset)), $"{file}: {asset}");
                references++;
            }
            if (!file.EndsWith(".css")) continue;
            foreach (Match match in Regex.Matches(content, "url\\(\\s*[\"']?(?<path>[^\"')]+)[\"']?\\s*\\)"))
            {
                var asset = match.Groups["path"].Value.Split('?', '#')[0];
                if (asset.Length == 0 || asset.StartsWith("data:") || Uri.TryCreate(asset, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") continue;
                var resolved = asset.StartsWith('/') ? Path.Combine(webRoot, asset.TrimStart('/')) : Path.Combine(Path.GetDirectoryName(file)!, asset);
                Assert.True(File.Exists(resolved), $"{file}: {asset}");
                references++;
            }
        }
        Assert.True(references > 40);
    }

    [Theory]
    [InlineData("H-IMG/aratehcourt.webp")]
    [InlineData("footerimg/phone.png")]
    [InlineData("siteLOGO/sitelogo.png")]
    [InlineData("sliderIMG/POP_Tennis_paddle.jpg")]
    public async Task LegacyImagePathsReturnOriginalBytes(string path)
    {
        var legacy = await app.Client.GetByteArrayAsync("/" + path);
        var organized = await app.Client.GetByteArrayAsync("/images/" + path);
        Assert.Equal(organized, legacy);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(int.MaxValue, 3)]
    public async Task ProjectPageBoundsRemainClamped(int requested, int expected)
    {
        var html = await Get($"/projects?page={requested}");
        var canonical = Attribute(Assert.Single(Tags(html, "<link\\b[^>]*rel=\"canonical\"[^>]*>")), "href");
        Assert.Equal(SeoApplication.Origin + "/projects" + (expected == 1 ? "" : $"?page={expected}"), canonical);
    }

    [Fact]
    public async Task LoadMoreBeyondLastPageDoesNotRepeatProjects()
    {
        var response = await app.Client.GetAsync("/projects/load-more?page=4");
        response.EnsureSuccessStatusCode();
        Assert.Equal("false", Assert.Single(response.Headers.GetValues("X-Has-More")));
        Assert.DoesNotContain("project-card", await response.Content.ReadAsStringAsync());
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

    [Fact]
    public async Task GuidePageReturns200WithBreadcrumbsAndSitemapEntry()
    {
        var (status, html) = await GetWithStatus("/padel-court-construction-guide");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("راهنمای جامع 0 تا 100 ساخت زمین پدل", WebUtility.HtmlDecode(html));
        Assert.Contains("BreadcrumbList", html);
    }

    [Fact]
    public async Task GuideChildrenAppearInNavAndSitemap()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var guide = db.Posts.Single(p => p.Slug == "padel-court-construction-guide");
        db.Posts.Add(new Post
        {
            Title = "دیواره‌های زمین پدل",
            Slug = "padel-court-walls-test",
            Content = "<p>دیواره‌های شیشه‌ای زمین پدل</p>",
            ParentPostId = guide.Id,
            CategoryId = guide.CategoryId,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var navHtml = await Get("/");
        Assert.Contains("/blog/padel-court-walls-test", navHtml);

        var sitemap = await Get("/sitemap.xml");
        Assert.Contains("/blog/padel-court-walls-test", sitemap);
        Assert.Contains("/padel-court-construction-guide", sitemap);
    }

    [Theory]
    [InlineData("/guides", "/padel-court-construction-guide")]
    [InlineData("/guides/index", "/padel-court-construction-guide")]
    [InlineData("/guides/padel-court-construction-guide", "/padel-court-construction-guide")]
    public async Task GuidesRoutes301ToSingleCanonicalPillarUrl(string path, string destination)
    {
        using var response = await app.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(destination, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task SitemapIncludesPublishedPillarGuidesAndCityPagesWithLastmod()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.PillarPages.Add(new PillarPage
        {
            Title = "راهنمای زمین پدل",
            Slug = "pillar-guide-sitemap-test",
            ContentHtml = "<p>محتوای راهنما</p>",
            IsPublished = true,
            CreatedAtUtc = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        db.CityPages.Add(new CityPage
        {
            Title = "زمین پدل ساری",
            Slug = "sari-sitemap-test",
            ContentHtml = "<p>محتوای شهر</p>",
            IsPublished = true,
            CreatedAtUtc = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        db.SaveChanges();

        var xml = XDocument.Parse(await Get("/sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlElements = xml.Root!.Elements(ns + "url").ToList();
        Assert.All(urlElements, e => Assert.True(
            e.Element(ns + "lastmod") != null,
            $"Missing lastmod on {e.Element(ns + "loc")?.Value}"));
        var urls = urlElements
            .Select(e => new
            {
                Loc = e.Element(ns + "loc")!.Value,
                Lastmod = e.Element(ns + "lastmod")!.Value
            })
            .ToDictionary(u => u.Loc, u => u.Lastmod);

        Assert.Contains($"{SeoApplication.Origin}/guides/pillar-guide-sitemap-test", urls.Keys);
        Assert.Equal("2025-03-01", urls[$"{SeoApplication.Origin}/guides/pillar-guide-sitemap-test"]);
        Assert.Contains($"{SeoApplication.Origin}/city/sari-sitemap-test", urls.Keys);
        Assert.Equal("2025-04-01", urls[$"{SeoApplication.Origin}/city/sari-sitemap-test"]);
    }

    [Theory]
    [InlineData("/about", "/about-us")]
    [InlineData("/contact", "/contact-us")]
    public async Task ShortCompanyRoutesRedirectPermanentlyToCanonical(string path, string destination)
    {
        using var response = await app.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(destination, response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/", "Home")]
    [InlineData("/blog", "Blog")]
    [InlineData("/projects", "Projects")]
    [InlineData("/about-us", "About")]
    [InlineData("/contact-us", "Contact")]
    [InlineData("/padel-court-construction-guide", "Guide")]
    public async Task ConfiguredMetaDescriptionOverridesPageFallback(string path, string page)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = await db.SiteSettings.SingleAsync(s => s.Id == 1);
        var property = settings.GetType().GetProperty($"Seo{page}Description")!;
        var original = property.GetValue(settings);
        var cache = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        property.SetValue(settings, "Custom editorial description");
        await db.SaveChangesAsync();
        cache.Remove("SiteSettingsProvider:Snapshot");
        try
        {
            var html = await Get(path);
            Assert.Contains("name=\"description\" content=\"Custom editorial description\"", html);
        }
        finally
        {
            property.SetValue(settings, original);
            await db.SaveChangesAsync();
            cache.Remove("SiteSettingsProvider:Snapshot");
        }
    }

    [Fact]
    public async Task BlogFirstPageCanonicalDoesNotPointToRedirect()
    {
        var html = await Get("/blog");
        Assert.Equal($"{SeoApplication.Origin}/blog", Canonical(html));
    }

    [Theory]
    [InlineData("/technical-docs", "/blog")]
    [InlineData("/technical-docs/download/1", "/blog")]
    [InlineData("/process", "/projects")]
    public async Task RetiredModulesRedirectPermanently(string path, string destination)
    {
        var response = await app.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(destination, response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(200, 1)]
    [InlineData(201, 2)]
    public void ArticleReadingUsesTwoHundredWordsPerMinute(int wordCount, int minutes)
    {
        var html = "<p>" + string.Join(" ", Enumerable.Repeat("پدل", wordCount)) + "</p>";
        Assert.Equal(minutes, final_p_01.Application.Services.ArticleReading.Prepare(html).Minutes);
    }

    [Fact]
    public void ArticleReadingPreservesAnchorsAndAvoidsGeneratedCollisions()
    {
        var reading = final_p_01.Application.Services.ArticleReading.Prepare("<h2 id=\"article-section-1\">ابعاد</h2><h3>دیوار &amp; شیشه</h3>");
        Assert.Equal(new[] { "article-section-1", "article-section-2" }, reading.Headings.Select(h => h.Id));
        Assert.Equal("دیوار & شیشه", reading.Headings[1].Title);
        Assert.Contains("id=\"article-section-2\"", reading.Html);
    }

    [Fact]
    public async Task ArticleIconsUseExistingLocalStylesheet()
    {
        var html = await Get("/padel-court-construction-guide");
        Assert.DoesNotContain("cdn.jsdelivr.net", html);
        Assert.Contains("/lib/bootstrap-icons/bootstrap-icons.css", html);
        using var response = await app.Client.GetAsync("/lib/bootstrap-icons/bootstrap-icons.css");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ArabicRoutesAreRetired()
    {
        // Arabic support was removed in 116478c: /ar must not serve an Arabic page
        // (unknown routes fall through to the soft-404 page, so accept any final status).
        var (_, html) = await GetWithStatus("/ar");
        Assert.DoesNotContain("<html lang=\"ar\"", html);
        Assert.DoesNotContain("hreflang=\"ar\"", html);
    }

    [Fact]
    public async Task NavigationComponentRendersInDesktopDrawerAndFooter()
    {
        // Classic 5-link nav restored in 116478c; the dynamic workspace-nav-subtree was removed.
        var html = await Get("/");
        Assert.DoesNotContain("workspace-nav-subtree", html);
        var desktop = Regex.Match(html, "<nav class=\"desktop-nav\">(.*?)</nav>", RegexOptions.Singleline).Value;
        var drawer = Regex.Match(html, "<div class=\"mobile-menu\".*?</nav>", RegexOptions.Singleline).Value;
        foreach (var section in new[] { desktop, drawer })
        {
            Assert.Contains("href=\"/about-us\"", section);
            Assert.Contains("href=\"/projects\"", section);
            Assert.Contains("href=\"/blog\"", section);
            Assert.Contains("href=\"/padel-court-construction-guide\"", section);
        }
        Assert.Equal(5, Regex.Matches(desktop, "<li>").Count);
    }

    [Fact]
    public async Task DesktopNavigationDoesNotDuplicateDynamicGuideAndBlogLinks()
    {
        var html = await Get("/");
        var desktop = Regex.Match(html, "<nav class=\"desktop-nav\">(.*?)</nav>", RegexOptions.Singleline).Value;
        Assert.Single(Tags(desktop, "<a\\b[^>]*href=\"/padel-court-construction-guide\"[^>]*>"));
        Assert.Single(Tags(desktop, "<a\\b[^>]*href=\"/blog\"[^>]*>"));
        Assert.DoesNotContain("nav-has-mega", desktop);
    }
}
