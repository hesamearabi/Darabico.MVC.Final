using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using final_p_01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;

namespace final_p_01.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IOutputCacheStore _outputCacheStore;

        // TODO: Owner to confirm mappings. Use /padel-court-construction-guide for guide topics, /blog for others.
        // Some examples below are mapped to 410 Gone to show how to handle permanently removed content.
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
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
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

            return View(latestPosts);
        }

        [Route("blog/{slug}")]
        [OutputCache(Duration = 3600, Tags = new[] { "BlogCache" })]
        public async Task<IActionResult> Post([FromRoute] string? slug, [FromQuery(Name = "slug")] string? querySlug)
        {
            var actualSlug = slug ?? querySlug;
            if (string.IsNullOrEmpty(actualSlug)) return base.NotFound();

            if (actualSlug.Equals("padel-court-construction-guide", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectPermanent("/padel-court-construction-guide");
            }

            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == actualSlug && p.IsPublished);

            if (post == null) return base.NotFound();

            var relatedPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Id != post.Id && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
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

            var viewModel = new PostDetailsViewModel
            {
                Post = post,
                RelatedPosts = relatedPosts
            };

            ViewData["Title"] = post.Title;
            if (!string.IsNullOrWhiteSpace(post.MetaDescription))
            {
                ViewData["MetaDescription"] = post.MetaDescription;
            }

            if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
            {
                ViewData["OgImage"] = $"https://{Request.Host}{post.CoverImagePath}";
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
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide");

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
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == "padel-court-construction-guide" && p.IsPublished);

            if (guidePost == null)
            {
                return base.NotFound();
            }

            var relatedPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Id != guidePost.Id)
                .OrderByDescending(p => p.CreatedAt)
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

            var viewModel = new PostDetailsViewModel
            {
                Post = guidePost,
                RelatedPosts = relatedPosts
            };

            ViewData["Title"] = guidePost.Title;
            if (!string.IsNullOrWhiteSpace(guidePost.MetaDescription))
            {
                ViewData["MetaDescription"] = guidePost.MetaDescription;
            }

            if (!string.IsNullOrWhiteSpace(guidePost.CoverImagePath))
            {
                ViewData["OgImage"] = $"https://{Request.Host}{guidePost.CoverImagePath}";
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
            if (LegacyRedirects.TryGetValue(actionName, out string targetPath))
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
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Slug,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.CoverImagePath
                })
                .ToListAsync();

            string baseUrl = ResolveBaseUrl();
            DateTime? newestPostDate = posts.Count > 0
                ? posts.Max(p => p.UpdatedAt ?? p.CreatedAt)
                : null;

            XNamespace sitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace imageNs = "http://www.google.com/schemas/sitemap-image/1.1";

            var urlset = new XElement(
                sitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", imageNs.NamespaceName));

            var staticPaths = new[]
            {
                "/",
                "/about-us",
                "/contact-us",
                "/blog",
                "/padel-court-construction-guide"
            };

            foreach (var path in staticPaths)
            {
                var urlElement = new XElement(
                    sitemapNs + "url",
                    new XElement(sitemapNs + "loc", BuildAbsoluteUrl(baseUrl, path)));

                if ((path == "/" || path == "/blog") && newestPostDate.HasValue)
                {
                    urlElement.Add(new XElement(sitemapNs + "lastmod", newestPostDate.Value.ToString("yyyy-MM-dd")));
                }

                urlset.Add(urlElement);
            }

            foreach (var post in posts)
            {
                if (string.IsNullOrWhiteSpace(post.Slug))
                {
                    continue;
                }

                string encodedSlug = EncodePathSegments(post.Slug);
                string postUrl = BuildAbsoluteUrl(baseUrl, $"/blog/{encodedSlug}");
                DateTime lastModified = post.UpdatedAt ?? post.CreatedAt;

                var urlElement = new XElement(
                    sitemapNs + "url",
                    new XElement(sitemapNs + "loc", postUrl),
                    new XElement(sitemapNs + "lastmod", lastModified.ToString("yyyy-MM-dd")));

                if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
                {
                    string absoluteImageUrl = post.CoverImagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                              post.CoverImagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        ? post.CoverImagePath
                        : BuildAbsoluteUrl(baseUrl, $"/{EncodePathSegments(post.CoverImagePath)}");

                    urlElement.Add(
                        new XElement(
                            imageNs + "image",
                            new XElement(imageNs + "loc", absoluteImageUrl)));
                }

                urlset.Add(urlElement);
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

        private string ResolveBaseUrl()
        {
            var configuredBaseUrl = _configuration["SiteSettings:BaseUrl"]?.Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return configuredBaseUrl;
            }

            var env = HttpContext?.RequestServices?.GetService<IWebHostEnvironment>();
            if (env?.IsDevelopment() == true && Request?.Host.HasValue == true)
            {
                return $"{Request.Scheme}://{Request.Host}";
            }

            return "https://darabico.com";
        }

        private static string BuildAbsoluteUrl(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return baseUrl;
            }

            return $"{baseUrl}/{path.TrimStart('/')}";
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

        [Route("Home/PageNotFound")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound(int statusCode)
        {
            Response.StatusCode = 404;
            ViewBag.StatusCode = statusCode;
            return View("NotFound");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}