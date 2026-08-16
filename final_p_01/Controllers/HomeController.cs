using System.Diagnostics;
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
        // کش موقتا غیرفعال شده تا مقالات جدید را در لحظه ببینید
        // [OutputCache(Duration = 3600)] 
        public async Task<IActionResult> Index()
        {
            var latestPosts = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(latestPosts);
        }

        // --- 2. صفحه ادامه مطلب مقاله ---
        // [OutputCache(Duration = 3600, VaryByRouteValueNames = new[] { "slug" })]
        // --- 2. صفحه ادامه مطلب مقاله ---
        // --- 2. صفحه ادامه مطلب مقاله ---
        public async Task<IActionResult> Post(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Faqs) // 🚀 این خط اضافه شد تا گارسون سوالات متداول را هم بیاورد
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (post == null) return NotFound();

            // گرفتن 4 مقاله آخر (به جز مقاله فعلی) برای سایدبار
            ViewBag.RecentPosts = await _context.Posts
                .Where(p => p.IsPublished && p.Id != post.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(post);
        }
        // --- 3. صفحه لیست کل بلاگ‌ها ---
        // [OutputCache(Duration = 3600)]
        public async Task<IActionResult> Blog()
        {
            var posts = await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
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
            // گرفتن مقالات از دیتابیس
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
            xml += "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n";

            var staticPages = new List<(string Url, string ChangeFreq, string Priority)>
            {
                ("https://darabico.com/", "daily", "1.0"),
                ("https://darabico.com/Home/About", "monthly", "0.8"),
                ("https://darabico.com/Home/Contact", "monthly", "0.8"),
                ("https://darabico.com/Home/Blog", "daily", "0.9")
            };

            // اضافه کردن صفحات ثابت
            foreach (var page in staticPages)
            {
                xml += "  <url>\n";
                xml += $"    <loc>{page.Url}</loc>\n";
                xml += $"    <changefreq>{page.ChangeFreq}</changefreq>\n";
                xml += $"    <priority>{page.Priority}</priority>\n";
                xml += "  </url>\n";
            }

            // اضافه کردن مقالات پویا
            foreach (var post in posts)
            {
                xml += "  <url>\n";
                xml += $"    <loc>https://darabico.com/Home/Post?slug={post.Slug}</loc>\n";
                xml += $"    <lastmod>{post.CreatedAt.ToString("yyyy-MM-dd")}</lastmod>\n";
                xml += "    <changefreq>weekly</changefreq>\n";
                xml += "    <priority>0.7</priority>\n";
                xml += "  </url>\n";
            }

            xml += "</urlset>";

            return Content(xml, "application/xml");
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