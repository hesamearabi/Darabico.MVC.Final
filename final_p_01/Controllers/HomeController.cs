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
            if (string.IsNullOrEmpty(actualSlug)) return NotFound();

            if (string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(querySlug))
            {
                return RedirectPermanent($"/blog/{actualSlug}");
            }

            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == actualSlug && p.IsPublished);

            if (post == null) return NotFound();

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

            return View(viewModel);
        }

        [Route("blog")]
        [Route("Home/Blog")]
        public async Task<IActionResult> Blog([FromQuery] int page = 1)
        {
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/Blog", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectPermanent(page > 1 ? $"/blog?page={page}" : "/blog");
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

        [Route("padel-court-construction-guide")]
        public async Task<IActionResult> PadelGuide()
        {
            var guidePost = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Slug == "padel-court-construction-guide" && p.IsPublished);

            if (guidePost == null)
            {
                return NotFound();
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
        public IActionResult About()
        {
            if (Request.Path.Value != null && Request.Path.Value.Contains("Home/about", StringComparison.OrdinalIgnoreCase))
                return RedirectPermanent("/about-us");

            return View("about");
        }

        public IActionResult PadelGrass() { return View("padelgrass"); }
        public IActionResult Structure() { return View("structure"); }
        public IActionResult Resume() { return View(); }
        public IActionResult CostArticle() { return View(); }
        public IActionResult GlassArticle() { return View(); }
        public IActionResult DimensionsArticle() { return View(); }
        public IActionResult RulesArticle() { return View(); }
        public IActionResult CoverArticle() { return View(); }
        public IActionResult ClubManagementArticle() { return View(); }

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            // فقط فیلدهای مورد نیاز واکشی می‌شوند
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Slug, p.CreatedAt })
                .ToListAsync();

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // تنظیم خودکار Base URL بر بستر HTTPS
            var baseUrl = $"https://{Request.Host}";

            var staticPages = new List<(string Url, string ChangeFreq, string Priority)>
            {
                ($"{baseUrl}/", "daily", "1.0"),
                ($"{baseUrl}/about-us", "monthly", "0.8"),
                ($"{baseUrl}/contact-us", "monthly", "0.8"),
                ($"{baseUrl}/blog", "daily", "0.9"),
                ($"{baseUrl}/padel-court-construction-guide", "weekly", "0.9")
            };

            foreach (var page in staticPages)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{page.Url}</loc>");
                xml.AppendLine($"    <changefreq>{page.ChangeFreq}</changefreq>");
                xml.AppendLine($"    <priority>{page.Priority}</priority>");
                xml.AppendLine("  </url>");
            }

            foreach (var post in posts)
            {
                var postUrl = $"{baseUrl}/blog/{post.Slug}";
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{postUrl}</loc>");
                xml.AppendLine($"    <lastmod>{post.CreatedAt:yyyy-MM-dd}</lastmod>");
                xml.AppendLine("    <changefreq>weekly</changefreq>");
                xml.AppendLine("    <priority>0.7</priority>");
                xml.AppendLine("  </url>");
            }

            xml.AppendLine("</urlset>");

            return Content(xml.ToString(), "application/xml");
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