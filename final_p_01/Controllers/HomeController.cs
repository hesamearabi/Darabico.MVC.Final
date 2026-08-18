using System.Diagnostics;
using System.Text;
using final_p_01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using final_p_01.Infrastructure.Data;

namespace final_p_01.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // --- 1. صفحه اصلی ---
        public async Task<IActionResult> Index()
        {
            var latestPosts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(latestPosts);
        }

        // --- 2. صفحه ادامه مطلب مقاله ---
        [Route("blog/{slug}")]
        [Route("Home/Post")]
        public async Task<IActionResult> Post([FromRoute] string? slug, [FromQuery(Name = "slug")] string? querySlug)
        {
            var actualSlug = slug ?? querySlug;
            if (string.IsNullOrEmpty(actualSlug)) return base.NotFound();

            // 🚀 BUG FIX: استفاده از مسیر متنی صریح برای جلوگیری از لوپ (Infinite Redirect Loop)
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

            return View(viewModel);
        }

        // --- 3. صفحه لیست کل بلاگ‌ها همراه با Pagination ---
        [Route("blog")]
        [Route("Home/Blog")]
        public async Task<IActionResult> Blog([FromQuery] int page = 1)
        {
            // 🚀 BUG FIX: استفاده از مسیر متنی صریح برای جلوگیری از لوپ
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/Blog", StringComparison.OrdinalIgnoreCase))
            {
                string targetUrl = page > 1 ? $"/blog?page={page}" : "/blog";
                return RedirectPermanent(targetUrl);
            }

            int pageSize = 9; // تعداد مقاله در هر صفحه
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

        // =========================================================
        // 🚀 صفحه مستقل (Main Page): راهنمای ۰ تا ۱۰۰ ساخت زمین پدل
        // =========================================================
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

        // --- صفحات استاتیک ---
        [Route("contact-us")]
        [Route("Home/Contact")]
        public IActionResult Contact()
        {
            // 🚀 BUG FIX: جلوگیری از لوپ ریدایرکت
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/Contact", StringComparison.OrdinalIgnoreCase))
                return RedirectPermanent("/contact-us");

            return View();
        }

        [Route("about-us")]
        [Route("Home/about")]
        public IActionResult about()
        {
            // 🚀 BUG FIX: جلوگیری از لوپ ریدایرکت
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

        // =========================================================
        // 🚀 4. SEO: تولید خودکار نقشه سایت (Dynamic Sitemap)
        // =========================================================
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
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

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
                    // 🚀 هماهنگ‌سازی آدرس‌های تولید شده در نقشه سایت با روت‌های تمیز جدید
                    string cleanUrl = page.Url
                        .Replace("/Home/Contact", "/contact-us", StringComparison.OrdinalIgnoreCase)
                        .Replace("/Home/about", "/about-us", StringComparison.OrdinalIgnoreCase)
                        .Replace("/Home/Blog", "/blog", StringComparison.OrdinalIgnoreCase);

                    xml.AppendLine("  <url>");
                    xml.AppendLine($"    <loc>{cleanUrl}</loc>");
                    xml.AppendLine($"    <changefreq>{page.ChangeFreq}</changefreq>");
                    xml.AppendLine($"    <priority>{page.Priority}</priority>");
                    xml.AppendLine("  </url>");
                }
            }

            foreach (var post in posts)
            {
                // تولید آدرس کاملاً منطبق با روت جدید
                var postUrl = $"{Request.Scheme}://{Request.Host}/blog/{post.Slug}";

                if (!string.IsNullOrEmpty(postUrl))
                {
                    xml.AppendLine("  <url>");
                    xml.AppendLine($"    <loc>{postUrl}</loc>");
                    xml.AppendLine($"    <lastmod>{post.CreatedAt:yyyy-MM-dd}</lastmod>");
                    xml.AppendLine("    <changefreq>weekly</changefreq>");
                    xml.AppendLine("    <priority>0.7</priority>");
                    xml.AppendLine("  </url>");
                }
            }

            xml.AppendLine("</urlset>");

            return Content(xml.ToString(), "application/xml");
        }

        // --- 5. مدیریت خطاها ---
        [Route("Home/PageNotFound")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound(int statusCode)
        {
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