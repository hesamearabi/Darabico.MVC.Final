using System.Diagnostics;
using System.Text; // 🚀 اضافه شده برای بهینه‌سازی حافظه
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
                .AsNoTracking() // 🚀 پرفورمنس: عدم ردگیری تغییرات در دیتابیس
                .Include(p => p.Category)
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(latestPosts);
        }

        // --- 2. صفحه ادامه مطلب مقاله ---
        public async Task<IActionResult> Post(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var post = await _context.Posts
                .AsNoTracking() // 🚀 پرفورمنس
                .Include(p => p.Category)
                .Include(p => p.Faqs)
                // 🚀 امنیت (رفع IDOR): حتماً بررسی می‌کنیم که مقاله منتشر شده باشد
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (post == null) return NotFound();

            ViewBag.RecentPosts = await _context.Posts
                .AsNoTracking() // 🚀 پرفورمنس
                .Where(p => p.IsPublished && p.Id != post.Id && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(post);
        }

        // --- 3. صفحه لیست کل بلاگ‌ها ---
        public async Task<IActionResult> Blog()
        {
            var posts = await _context.Posts
                .AsNoTracking() // 🚀 پرفورمنس
                .Include(p => p.Category)
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        // =========================================================
        // 🚀 صفحه مستقل (Main Page): راهنمای ۰ تا ۱۰۰ ساخت زمین پدل
        // =========================================================
        [Route("padel-court-construction-guide")]
        public async Task<IActionResult> PadelGuide()
        {
            var guidePost = await _context.Posts
                .AsNoTracking() // 🚀 پرفورمنس
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == "padel-court-construction-guide" && p.IsPublished);

            if (guidePost == null)
            {
                return NotFound();
            }

            return View(guidePost);
        }

        // --- صفحات استاتیک قبلی شما ---
        public IActionResult Contact() { return View(); }
        public IActionResult about() { return View(); }
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

            // 🚀 پرفورمنس: استفاده از StringBuilder برای جلوگیری از نشت حافظه (Memory Leak)
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            var staticPages = new List<(string Url, string ChangeFreq, string Priority)>
            {
                ("https://darabico.com/", "daily", "1.0"),
                ("https://darabico.com/Home/About", "monthly", "0.8"),
                ("https://darabico.com/Home/Contact", "monthly", "0.8"),
                ("https://darabico.com/Home/Blog", "daily", "0.9"),
                ("https://darabico.com/padel-court-construction-guide", "weekly", "0.9") // 🚀 اضافه شدن لینک راهنما به نقشه سایت
            };

            // اضافه کردن صفحات ثابت
            foreach (var page in staticPages)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{page.Url}</loc>");
                xml.AppendLine($"    <changefreq>{page.ChangeFreq}</changefreq>");
                xml.AppendLine($"    <priority>{page.Priority}</priority>");
                xml.AppendLine("  </url>");
            }

            // اضافه کردن مقالات پویا
            foreach (var post in posts)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>https://darabico.com/Home/Post?slug={post.Slug}</loc>");
                xml.AppendLine($"    <lastmod>{post.CreatedAt:yyyy-MM-dd}</lastmod>");
                xml.AppendLine("    <changefreq>weekly</changefreq>");
                xml.AppendLine("    <priority>0.7</priority>");
                xml.AppendLine("  </url>");
            }

            xml.AppendLine("</urlset>");

            return Content(xml.ToString(), "application/xml");
        }

        // --- 5. مدیریت خطاها ---
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound(int statusCode)
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