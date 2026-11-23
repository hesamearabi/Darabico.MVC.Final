using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers
{
    public class HomeController : Controller
    {
        private const string GuideSlug = "padel-court-construction-guide";

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IOutputCacheStore _outputCacheStore;

        private static readonly Dictionary<string, string> LegacyRedirects = new(StringComparer.OrdinalIgnoreCase)
        {
            { "padelgrass", "/padel-court-construction-guide" },
            { "structure", "/padel-court-construction-guide" },
            { "Resume", "/about-us" },
            { "CostArticle", "/padel-court-construction-guide" },
            { "GlassArticle", "/padel-court-construction-guide" },
            { "DimensionsArticle", "/padel-court-construction-guide" },
            { "RulesArticle", "/blog" },
            { "CoverArticle", "/padel-court-construction-guide" },
            { "ClubManagementArticle", "410" }
        };

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration, IOutputCacheStore outputCacheStore)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _outputCacheStore = outputCacheStore;
        }

        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> Index()
        {
            int homePagePostsCount = _configuration.GetValue<int>("SiteSettings:HomePagePostsCount", 6);

            var latestPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != GuideSlug)
                .OrderByDescending(p => p.CreatedAt)
                .Take(homePagePostsCount)
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    CoverImagePath = p.CoverImagePath,
                    CoverImageAlt = p.CoverImageAlt,
                    MetaDescription = p.MetaDescription,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category != null ? p.Category.Name : null
                })
                .ToListAsync();

            var estimatorSettings = await _context.EstimatorSettings.AsNoTracking()
                .Include(s => s.MaterialTiers)
                .Include(s => s.CityMultipliers)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == final_p_01.Domain.Entities.EstimatorSettings.SingletonId);
            ViewData["Estimator"] = new EstimatorFormViewModel
            {
                MaterialTiers = estimatorSettings?.MaterialTiers.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList() ?? new(),
                CityMultipliers = estimatorSettings?.CityMultipliers.Where(c => c.IsActive).OrderBy(c => c.City).ToList() ?? new(),
                Disclaimer = estimatorSettings?.Disclaimer
            };
            return View(latestPosts);
        }

        [Route("blog/{slug}")]
        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> Post([FromRoute] string? slug, [FromQuery(Name = "slug")] string? querySlug)
        {
            var actualSlug = (slug ?? querySlug)?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(actualSlug)) return base.NotFound();

            if (actualSlug.Equals(GuideSlug, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectPermanent("/padel-court-construction-guide");
            }

            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == actualSlug && p.IsPublished);

            if (post == null)
            {
                var redirectedSlug = await _context.PostSlugHistories
                    .AsNoTracking()
                    .Where(h => h.Slug == actualSlug && h.Post.IsPublished)
                    .Select(h => h.Post.Slug)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(redirectedSlug))
                {
                    if (redirectedSlug.Equals(GuideSlug, StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectPermanent("/padel-court-construction-guide");
                    }

                    string encodedTargetSlug = EncodePathSegments(redirectedSlug.Trim().ToLowerInvariant());
                    return RedirectPermanent($"/blog/{encodedTargetSlug}");
                }

                return base.NotFound();
            }

            var relatedPosts = await GetRelatedPostsAsync(post.Id, post.CategoryId);

            var viewModel = new PostDetailsViewModel
            {
                Post = post,
                RelatedPosts = relatedPosts
            };

            ViewData["Title"] = post.Title;
            ViewData["MetaDescription"] = ResolveMetaDescription(post.MetaDescription, post.Content, post.Title);

            if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
            {
                ViewData["OgImage"] = BuildAbsoluteUrl(ResolveBaseUrl(), post.CoverImagePath);
            }

            return View(viewModel);
        }

        [Route("blog")]
        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> Blog([FromQuery] int page = 1)
        {
            if (Request.Query.ContainsKey("page") && page <= 1)
            {
                return RedirectPermanent("/blog");
            }

            int pageSize = _configuration.GetValue<int>("SiteSettings:BlogPageSize", 9);

            var query = _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != GuideSlug);

            int totalPosts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            if (page > 1 && page > totalPages)
            {
                return base.NotFound();
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    CoverImagePath = p.CoverImagePath,
                    CoverImageAlt = p.CoverImageAlt,
                    MetaDescription = p.MetaDescription,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category != null ? p.Category.Name : null
                })
                .ToListAsync();

            var viewModel = new BlogListViewModel
            {
                Posts = posts,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [Route("padel-court-construction-guide")]
        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> PadelGuide()
        {
            var guidePost = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == GuideSlug && p.IsPublished);

            if (guidePost == null)
            {
                return base.NotFound();
            }

            var relatedPosts = await GetRelatedPostsAsync(guidePost.Id, guidePost.CategoryId);

            var viewModel = new PostDetailsViewModel
            {
                Post = guidePost,
                RelatedPosts = relatedPosts
            };

            ViewData["Title"] = guidePost.Title;
            ViewData["MetaDescription"] = ResolveMetaDescription(guidePost.MetaDescription, guidePost.Content, guidePost.Title);

            if (!string.IsNullOrWhiteSpace(guidePost.CoverImagePath))
            {
                ViewData["OgImage"] = BuildAbsoluteUrl(ResolveBaseUrl(), guidePost.CoverImagePath);
            }

            return View(viewModel);
        }

        [Route("contact-us")]
        public IActionResult Contact()
        {
            return View();
        }

        [Route("about-us")]
        public IActionResult about()
        {
            return View();
        }

        [Route("Home/{actionName}")]
        public IActionResult LegacyRedirect(string actionName)
        {
            if (LegacyRedirects.TryGetValue(actionName, out string? targetPath) && !string.IsNullOrEmpty(targetPath))
            {
                if (targetPath == "410")
                {
                    return StatusCode(410);
                }
                return RedirectPermanent(targetPath);
            }

            return base.NotFound();
        }

        [Route("sitemap.xml")]
        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> Sitemap()
        {
            var allPublishedPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Slug,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.CoverImagePath
                })
                .ToListAsync();

            var publishedProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .Select(p => new { p.Slug, CreatedAt = p.CreatedAtUtc, UpdatedAt = p.UpdatedAtUtc })
                .ToListAsync(HttpContext.RequestAborted);

            var guidePost = allPublishedPosts.FirstOrDefault(p => string.Equals(p.Slug, GuideSlug, StringComparison.OrdinalIgnoreCase));
            var blogPosts = allPublishedPosts
                .Where(p => !string.Equals(p.Slug, GuideSlug, StringComparison.OrdinalIgnoreCase))
                .ToList();

            string baseUrl = ResolveBaseUrl();

            DateTime? newestSiteDate = allPublishedPosts.Count > 0
                ? allPublishedPosts.Max(p => p.UpdatedAt ?? p.CreatedAt)
                : null;

            DateTime? newestBlogDate = blogPosts.Count > 0
                ? blogPosts.Max(p => p.UpdatedAt ?? p.CreatedAt)
                : null;

            XNamespace sitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace imageNs = "http://www.google.com/schemas/sitemap-image/1.1";

            var urlset = new XElement(
                sitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", imageNs.NamespaceName));

            var homeElement = new XElement(
                sitemapNs + "url",
                new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, "/")));
            if (newestSiteDate.HasValue)
            {
                homeElement.Add(new XElement(sitemapNs + "lastmod", newestSiteDate.Value.ToString("yyyy-MM-dd")));
            }
            urlset.Add(homeElement);

            urlset.Add(new XElement(
                sitemapNs + "url",
                new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, "/about-us"))));

            urlset.Add(new XElement(
                sitemapNs + "url",
                new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, "/contact-us"))));

            var blogElement = new XElement(
                sitemapNs + "url",
                new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, "/blog")));
            if (newestBlogDate.HasValue)
            {
                blogElement.Add(new XElement(sitemapNs + "lastmod", newestBlogDate.Value.ToString("yyyy-MM-dd")));
            }
            urlset.Add(blogElement);

            if (guidePost != null)
            {
                DateTime guideLastModified = guidePost.UpdatedAt ?? guidePost.CreatedAt;
                var guideElement = new XElement(
                    sitemapNs + "url",
                    new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, "/padel-court-construction-guide")),
                    new XElement(sitemapNs + "lastmod", guideLastModified.ToString("yyyy-MM-dd")));

                if (!string.IsNullOrWhiteSpace(guidePost.CoverImagePath))
                {
                    guideElement.Add(
                        new XElement(
                            imageNs + "image",
                            new XElement(imageNs + "loc", BuildAbsoluteUrl(baseUrl, guidePost.CoverImagePath))));
                }

                urlset.Add(guideElement);
            }

            foreach (var post in blogPosts)
            {
                if (string.IsNullOrWhiteSpace(post.Slug))
                {
                    continue;
                }

                string normalizedSlug = post.Slug.Trim().ToLowerInvariant();
                string encodedSlug = EncodePathSegments(normalizedSlug);
                string postUrl = BuildAbsoluteUrl(baseUrl, $"/blog/{encodedSlug}");
                DateTime lastModified = post.UpdatedAt ?? post.CreatedAt;

                var urlElement = new XElement(
                    sitemapNs + "url",
                    new XElement(sitemapNs + "loc", postUrl),
                    new XElement(sitemapNs + "lastmod", lastModified.ToString("yyyy-MM-dd")));

                if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
                {
                    urlElement.Add(
                        new XElement(
                            imageNs + "image",
                            new XElement(imageNs + "loc", BuildAbsoluteUrl(baseUrl, post.CoverImagePath))));
                }

                urlset.Add(urlElement);
            }

            foreach (var path in new[] { "/projects", "/estimator", "/inquiry" })
            {
                urlset.Add(new XElement(sitemapNs + "url",
                    new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, path))));
            }

            foreach (var project in publishedProjects)
            {
                urlset.Add(new XElement(sitemapNs + "url",
                    new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, $"/projects/{Uri.EscapeDataString(project.Slug)}")),
                    new XElement(sitemapNs + "lastmod", (project.UpdatedAt ?? project.CreatedAt).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))));
            }

            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);

            await using var memoryStream = new MemoryStream();
            var writerSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                Async = true
            };

            await using (var xmlWriter = XmlWriter.Create(memoryStream, writerSettings))
            {
                await document.SaveAsync(xmlWriter, HttpContext.RequestAborted);
            }

            string xmlContent = Encoding.UTF8.GetString(memoryStream.ToArray());
            return Content(xmlContent, "application/xml", Encoding.UTF8);
        }

        private async Task<List<PostCardViewModel>> GetRelatedPostsAsync(int currentPostId, int? categoryId)
        {
            var query = _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Id != currentPostId && p.Slug != GuideSlug);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                int targetCategoryId = categoryId.Value;
                query = query
                    .OrderByDescending(p => p.CategoryId == targetCategoryId)
                    .ThenByDescending(p => p.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            return await query
                .Take(4)
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    CoverImagePath = p.CoverImagePath,
                    CoverImageAlt = p.CoverImageAlt,
                    MetaDescription = p.MetaDescription,
                    CreatedAt = p.CreatedAt,
                    CategoryName = p.Category != null ? p.Category.Name : null
                })
                .ToListAsync();
        }

        private static string ResolveMetaDescription(string? metaDescription, string? htmlContent, string? fallbackTitle)
        {
            if (!string.IsNullOrWhiteSpace(metaDescription))
            {
                return metaDescription.Trim();
            }

            if (!string.IsNullOrWhiteSpace(htmlContent))
            {
                string plainText = Regex.Replace(htmlContent, "<[^>]+>", " ");
                plainText = WebUtility.HtmlDecode(plainText);
                plainText = Regex.Replace(plainText, "\\s+", " ").Trim();

                if (!string.IsNullOrWhiteSpace(plainText))
                {
                    if (plainText.Length <= 155)
                    {
                        return plainText;
                    }

                    int lastSpace = plainText.LastIndexOf(' ', 152);
                    return (lastSpace > 80 ? plainText.Substring(0, lastSpace) : plainText.Substring(0, 152)).TrimEnd() + "...";
                }
            }

            return fallbackTitle ?? string.Empty;
        }

        [HttpGet("/robots.txt")]
        public IActionResult Robots([FromServices] final_p_01.Services.ISiteUrlBuilder urlBuilder)
        {
            return Content($"User-agent: *\nDisallow: /Admin/\nDisallow: /admin/\n\nSitemap: {urlBuilder.Absolute("/sitemap.xml")}\n", "text/plain", Encoding.UTF8);
        }

        private string ResolveBaseUrl()
        {
            return new final_p_01.Services.SiteUrlBuilder(_configuration).Absolute("/").TrimEnd('/');
        }

        private static string BuildAbsoluteUrl(string baseUrl, string path)
        {
            var baseUri = new Uri($"{baseUrl.TrimEnd('/')}/");

            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return baseUri.AbsoluteUri;
            }

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.TryCreate(path, UriKind.Absolute, out var externalUri)
                    ? externalUri.AbsoluteUri
                    : path;
            }

            string relativePath = "/" + path.TrimStart('/');
            return new Uri(baseUri, relativePath).AbsoluteUri;
        }

        private static string EncodePathSegments(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", segments.Select(Uri.EscapeDataString));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("RatingRateLimit")]
        public async Task<IActionResult> SubmitRating(int postId, int rating)
        {
            if (postId <= 0 || rating < 1 || rating > 5)
                return BadRequest(new { success = false, message = "مقادیر نامعتبر است." });

            var isPublished = await _context.Posts
                .Where(p => p.Id == postId)
                .Select(p => p.IsPublished)
                .FirstOrDefaultAsync();

            if (!isPublished)
                return NotFound(new { success = false, message = "مقاله یافت نشد." });

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var hashInput = Encoding.UTF8.GetBytes($"{clientIp}|{userAgent}|{postId}");
            var hashBytes = SHA256.HashData(hashInput);
            var hashString = Convert.ToBase64String(hashBytes);
            var cookieName = $"rated_cookie_{postId}";

            if (Request.Cookies.ContainsKey(cookieName) && Request.Cookies[cookieName] == hashString)
            {
                return Json(new { success = false, message = "شما قبلاً به این مقاله رای داده‌اید." });
            }

            try
            {
                await _context.Posts
                    .Where(p => p.Id == postId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.AverageRating, p => Math.Round(((p.AverageRating * p.TotalVotes) + rating) / (p.TotalVotes + 1.0), 1))
                        .SetProperty(p => p.TotalVotes, p => p.TotalVotes + 1));

                var updatedStats = await _context.Posts
                    .Where(p => p.Id == postId)
                    .Select(p => new { p.AverageRating, p.TotalVotes })
                    .FirstOrDefaultAsync();

                await _outputCacheStore.EvictByTagAsync("BlogCache", default);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(365),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append(cookieName, hashString, cookieOptions);

                return Json(new
                {
                    success = true,
                    newAverage = updatedStats?.AverageRating,
                    newTotal = updatedStats?.TotalVotes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating for post {PostId}", postId);
                return StatusCode(500, new { success = false, message = "خطای سرور در ثبت امتیاز." });
            }
        }

        [Route("home/pagenotfound")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound(int statusCode = 404)
        {
            int resolvedStatusCode = statusCode >= 400 && statusCode <= 599 ? statusCode : 404;
            Response.StatusCode = resolvedStatusCode;
            Response.Headers["X-Robots-Tag"] = "noindex, follow";
            ViewData["Robots"] = "noindex, follow";
            ViewBag.StatusCode = resolvedStatusCode;
            return resolvedStatusCode is 400 or 429
                ? View("StatusCode", resolvedStatusCode)
                : View("NotFound");
        }

        [Route("home/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            ViewData["Robots"] = "noindex, nofollow";
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}