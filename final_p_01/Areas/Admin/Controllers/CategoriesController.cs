using System.Linq;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // قفل امنیتی
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // جلوگیری از کش شدن و نمایش فوری
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // ۱. لیست دسته‌بندی‌ها
        // ==========================================
        [HttpGet]
        public IActionResult Index()
        {
            var categories = _context.Categories
                .AsNoTracking() // معجزه نمایش فوری
                .OrderByDescending(c => c.Id)
                .ToList();

            return View(categories);
        }

        // ==========================================
        // ۲. ساخت دسته‌بندی جدید
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            ModelState.Remove("Posts"); // خنثی کردن گیرهای دات‌نت

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // ==========================================
        // ۳. ویرایش دسته‌بندی (رفع ارور ۴۰۴)
        // ==========================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            ModelState.Remove("Posts");

            if (ModelState.IsValid)
            {
                var existingCategory = _context.Categories.Find(id);
                if (existingCategory == null) return NotFound();

                // آپدیت نام و اسلاگ
                existingCategory.Name = category.Name;
                existingCategory.Slug = category.Slug;

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // ==========================================
        // ۴. حذف دسته‌بندی (محافظت شده در برابر کرش)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            // اینجا دات‌نت را می‌فرستیم چک کند آیا مقاله‌ای به این دسته‌بندی وصل است یا نه
            var category = _context.Categories.Include(c => c.Posts).FirstOrDefault(c => c.Id == id);

            if (category != null)
            {
                // اگر حتی یک مقاله داخل این دسته‌بندی بود، اخطار بده و حذف نکن!
                if (category.Posts != null && category.Posts.Any())
                {
                    TempData["ErrorMessage"] = $"نمی‌توانید '{category.Name}' را حذف کنید! ابتدا مقالاتِ داخل این دسته‌بندی را پاک کرده یا تغییر دهید.";
                    return RedirectToAction("Index");
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}