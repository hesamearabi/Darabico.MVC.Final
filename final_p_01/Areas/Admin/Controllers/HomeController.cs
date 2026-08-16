using System.Linq;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // فقط مدیران لاگین شده حق دیدن داشبورد را دارند
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // جمع‌آوری آمارهای کلیدی برای نمایش در داشبورد
            ViewBag.TotalPosts = _context.Posts.Count();
            ViewBag.PublishedPosts = _context.Posts.Count(p => p.IsPublished);
            ViewBag.TotalCategories = _context.Categories.Count();

            return View();
        }
    }
}