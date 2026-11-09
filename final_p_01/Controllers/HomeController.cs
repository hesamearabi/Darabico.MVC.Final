using System.Diagnostics;
using System.Text;
using final_p_01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using final_p_01.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            { "ClubManagementArticle", "410" } // Example: Content permanently removed
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
                .Include(p => p.Category)
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Take(homePagePostsCount)
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
            int pageSize = _configuration.GetValue<int>("SiteSettings:BlogPageSize", 9);

            var query = _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide");

            int totalPosts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var posts = await query
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                .ToListAsync();

            var viewModel = new PostDetailsViewModel
            {
                Post = guidePost,
                RelatedPosts = relatedPosts
            };

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
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");

            var staticPages = new List<(string? Url, string ChangeFreq, string Priority)>
            {
                (Url.Action("Index", "Home", null, Request.Scheme), "daily", "1.0"),
                (Url.Action("about", "Home", null, Request.Scheme), "monthly", "0.8"),
                (Url.Action("Contact", "Home", null, Request.Scheme), "monthly", "0.8"),
                (Url.Action("Blog", "Home", null, Request.Scheme), "daily", "0.9"),
                (Url.Action("PadelGuide", "Home", null, Request.Scheme), "weekly", "0.9")
            };

            foreach (var page in staticPages)
            {
                if (!string.IsNullOrEmpty(page.Url))
                {
                    string cleanUrl = page.Url
                        .Replace("/Home/Contact", "/contact-us", StringComparison.OrdinalIgnoreCase)
                        .Replace("/Home/about", "/about-us", StringComparison.OrdinalIgnoreCase)
                        .Replace("/Home/Blog", "/blog", StringComparison.OrdinalIgnoreCase);

                    if (cleanUrl.StartsWith("http://")) cleanUrl = cleanUrl.Replace("http://", "https://");

                    xml.AppendLine("  <url>");
                    xml.AppendLine($"    <loc>{cleanUrl}</loc>");
                    xml.AppendLine($"    <changefreq>{page.ChangeFreq}</changefreq>");
                    xml.AppendLine($"    <priority>{page.Priority}</priority>");
                    xml.AppendLine("  </url>");
                }
            }

            foreach (var post in posts)
            {
                var postUrl = $"https://{Request.Host}/blog/{post.Slug}";

                if (!string.IsNullOrEmpty(postUrl))
                {
                    xml.AppendLine("  <url>");
                    xml.AppendLine($"    <loc>{postUrl}</loc>");
                    xml.AppendLine($"    <lastmod>{post.CreatedAt:yyyy-MM-dd}</lastmod>");
                    xml.AppendLine("    <changefreq>weekly</changefreq>");
                    xml.AppendLine("    <priority>0.7</priority>");

                    if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
                    {
                        string absoluteImageUrl = $"https://{Request.Host}{post.CoverImagePath}";
                        xml.AppendLine("    <image:image>");
                        xml.AppendLine($"      <image:loc>{absoluteImageUrl}</image:loc>");
                        if (!string.IsNullOrWhiteSpace(post.CoverImageAlt))
                        {
                            xml.AppendLine($"      <image:title>{post.CoverImageAlt}</image:title>");
                        }
                        xml.AppendLine("    </image:image>");
                    }

                    xml.AppendLine("  </url>");
                }
            }

            xml.AppendLine("</urlset>");

            return Content(xml.ToString(), "application/xml");
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