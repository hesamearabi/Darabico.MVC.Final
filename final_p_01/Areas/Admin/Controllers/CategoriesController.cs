using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching; // 🚀 اضافه شده برای کنترل کش سرور

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // غیرفعال‌سازی کش مرورگر
    [OutputCache(NoStore = true)] // 🚀 FIX: غیرفعال‌سازی قطعی کش سرور برای جلوگیری از تاخیر در نمایش
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GenerateSeoFriendlySlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\u0600-\u06FF\-]+", "");
            text = Regex.Replace(text, @"-+", "-");
            return text.Trim('-');
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Posts)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(categories);
        }

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
            ModelState.Remove("Slug");

            category.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(category.Slug) ? category.Name : category.Slug);

            if (ModelState.IsValid)
            {
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
            ModelState.Remove("Slug");

            category.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(category.Slug) ? category.Name : category.Slug);

            if (ModelState.IsValid)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                bool hasPosts = await _context.Posts.AnyAsync(p => p.CategoryId == id);

                if (hasPosts)
                {
                    TempData["ErrorMessage"] = $"نمی‌توانید '{category.Name}' را حذف کنید! ابتدا مقالاتِ داخل این دسته‌بندی را پاک کرده یا تغییر دهید.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "دسته‌بندی با موفقیت حذف شد.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}