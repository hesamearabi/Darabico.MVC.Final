using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        // 🚀 SEO Helper: متد استانداردسازی اسلاگ برای جلوگیری از خطای دیتابیس و URLهای خراب
        private string GenerateSeoFriendlySlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\u0600-\u06FF\-]+", "");
            text = Regex.Replace(text, @"-+", "-");
            return text.Trim('-');
        }

        // ==========================================
        // ۱. لیست دسته‌بندی‌ها
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 🚀 پرفورمنس: تبدیل به Async/Await
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Posts)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

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
        public async Task<IActionResult> Create(Category category)
        {
            ModelState.Remove("Posts");

            if (ModelState.IsValid)
            {
                // تولید خودکار اسلاگ سئو در صورت خالی بودن
                category.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(category.Slug) ? category.Name : category.Slug);

                // 🚀 جلوگیری قطعی از کِرَش دیتابیس (جلوگیری از نقض ایندکس IsUnique)
                bool exists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug);
                if (exists)
                {
                    ModelState.AddModelError("Slug", "این دسته‌بندی (یا اسلاگ آن) قبلاً ثبت شده است.");
                    return View(category);
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "دسته‌بندی با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // ==========================================
        // ۳. ویرایش دسته‌بندی
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            ModelState.Remove("Posts");

            if (ModelState.IsValid)
            {
                category.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(category.Slug) ? category.Name : category.Slug);

                // بررسی یکتایی اسلاگ به جز دسته‌بندی فعلی
                bool exists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != id);
                if (exists)
                {
                    ModelState.AddModelError("Slug", "این اسلاگ (URL) توسط دسته‌بندی دیگری استفاده شده است.");
                    return View(category);
                }

                var existingCategory = await _context.Categories.FindAsync(id);
                if (existingCategory == null) return NotFound();

                existingCategory.Name = category.Name;
                existingCategory.Slug = category.Slug;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تغییرات با موفقیت ذخیره شد.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // ==========================================
        // ۴. حذف دسته‌بندی (محافظت شده در برابر کرش)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // 🚀 پرفورمنس (Memory Fix): بررسی Async بدون لود کردن جداول سنگین
                bool hasPosts = await _context.Posts.AnyAsync(p => p.CategoryId == id);

                if (hasPosts)
                {
                    TempData["ErrorMessage"] = $"نمی‌توانید '{category.Name}' را حذف کنید! ابتدا مقالاتِ داخل این دسته‌بندی را پاک کرده یا تغییر دهید.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "دسته‌بندی حذف شد.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}