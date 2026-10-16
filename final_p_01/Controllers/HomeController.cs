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

namespace final_p_01.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            int homePagePostsCount = _configuration.GetValue<int>("SiteSettings:HomePagePostsCount", 6);

            var latestPosts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                // مقاله راهنمای جامع پدل از لیست بلاگ‌های صفحه اصلی مستثنی شده است
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Take(homePagePostsCount)
                .ToListAsync();

            return View(latestPosts);
        }

        [Route("blog/{slug}")]
        [Route("Home/Post")]
        public async Task<IActionResult> Post([FromRoute] string? slug, [FromQuery(Name = "slug")] string? querySlug)
        {
            var actualSlug = slug ?? querySlug;
            if (string.IsNullOrEmpty(actualSlug)) return base.NotFound();

            // 🚀 SEO FIX: Redirect 301 Permanent از مسیر قدیمی وبلاگ به مسیر Pillar مستقل
            if (actualSlug.Equals("padel-court-construction-guide", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectPermanent("/padel-court-construction-guide");
            }

            if (string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(querySlug))
            {
                return RedirectPermanent($"/blog/{actualSlug}");
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

            // 🚀 SEO FIX: ارسال عکس مقاله به سمت View برای پر کردن متاتگ‌های og:image در _Layout
            if (!string.IsNullOrWhiteSpace(post.CoverImagePath))
            {
                ViewData["OgImage"] = $"https://{Request.Host}{post.CoverImagePath}";
            }

            return View(viewModel);
        }

        [Route("blog")]
        [Route("Home/Blog")]
        public async Task<IActionResult> Blog([FromQuery] int page = 1)
        {
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/Blog", StringComparison.OrdinalIgnoreCase))
            {
                string targetUrl = page > 1 ? $"/blog?page={page}" : "/blog";
                return RedirectPermanent(targetUrl);
            }

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

        // 🚀 URL جدید، تمیز و مستقل برای مقاله اصلی
        [Route("padel-court-construction-guide")]
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

            return View(guidePost);
        }

        [Route("contact-us")]
        [Route("Home/Contact")]
        public IActionResult Contact()
        {
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/Contact", StringComparison.OrdinalIgnoreCase))
                return RedirectPermanent("/contact-us");

            return View();
        }

        [Route("about-us")]
        [Route("Home/about")]
        public IActionResult about()
        {
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/about", StringComparison.OrdinalIgnoreCase))
                return RedirectPermanent("/about-us");

            return View();
        }

        public IActionResult padelgrass() { return View(); }
        public IActionResult structure() { return View(); }
        public IActionResult Resume() { return View(); }
        public IActionResult CostArticle() { return View(); }
        public IActionResult GlassArticle() { return View(); }
        public IActionResult DimensionsArticle() { return View(); }
        public IActionResult RulesArticle() { return View(); }
        public IActionResult CoverArticle() { return View(); }
        public IActionResult ClubManagementArticle() { return View(); }

        // 🚀 SEO FIX: تولید سایت‌مپ کاملاً داینامیک همراه با تگ‌های عکس (Image Sitemap)
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
            // اضافه کردن namespace برای عکس‌ها تا گوگل تصاویر مقالات را نیز ایندکس کند
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

                    // اگر مقاله عکس دارد، عکس آن هم به نقشه سایت اضافه می‌شود (بسیار عالی برای سئو)
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

        // 🚀 متد ثبت امتیاز (ستاره‌دهی) برای مقالات
        [HttpPost]
        public async Task<IActionResult> SubmitRating(int postId, int rating)
        {
            // اعتبارسنجی اولیه
            if (postId <= 0 || rating < 1 || rating > 5)
                return Json(new { success = false, message = "مقادیر نامعتبر است." });

            // پیدا کردن مقاله در دیتابیس
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return Json(new { success = false, message = "مقاله یافت نشد." });

            try
            {
                // محاسبه میانگین جدید
                double currentTotalScore = post.AverageRating * post.TotalVotes;
                post.TotalVotes += 1;
                post.AverageRating = Math.Round((currentTotalScore + rating) / post.TotalVotes, 1);

                // آپدیت دیتابیس
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    newAverage = post.AverageRating,
                    newTotal = post.TotalVotes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating for post {PostId}", postId);
                return Json(new { success = false, message = "خطای سرور در ثبت امتیاز." });
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