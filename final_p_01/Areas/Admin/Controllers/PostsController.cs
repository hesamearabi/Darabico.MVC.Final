using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🚀 متد کمکی برای آپلود ایمن تصاویر (ممانعت قطعی از RCE و آپلود فایل‌های مخرب)
        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            // 1. بررسی سایز (حداکثر 3 مگابایت)
            const long maxFileSize = 3 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new InvalidOperationException("حجم عکس نباید بیشتر از 3 مگابایت باشد.");

            // 2. بررسی پسوند مجاز
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                throw new InvalidOperationException("فرمت فایل غیرمجاز است. فقط (jpg, png, webp, gif) پذیرفته می‌شود.");

            // 3. بررسی اولیه نوع فایل (MIME)
            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("فایل آپلودی نامعتبر است.");

            // 4. تغییر نام قطعی برای جلوگیری از Path Traversal
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subFolder);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 5. آپلود به صورت Async
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile? coverImage)
        {
            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("Faqs");

            if (ModelState.IsValid)
            {
                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده است. لطفاً کلمه دیگری اضافه کنید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                try
                {
                    // آپلود ایمن
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        post.CoverImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("CoverImagePath", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                post.CreatedAt = DateTime.UtcNow; // استفاده از UTC برای استانداردسازی زمان
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Faqs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, IFormFile? coverImage)
        {
            if (id != post.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("Faqs");

            if (ModelState.IsValid)
            {
                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.Id != post.Id);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده است. لطفاً آن را تغییر دهید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                var existingPost = await _context.Posts.Include(p => p.Faqs).FirstOrDefaultAsync(p => p.Id == id);
                if (existingPost == null) return NotFound();

                existingPost.Title = post.Title;
                existingPost.Slug = post.Slug;
                existingPost.Content = post.Content;
                existingPost.CategoryId = post.CategoryId;
                existingPost.MetaDescription = post.MetaDescription;
                existingPost.IsPublished = post.IsPublished;

                try
                {
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        // 🚀 حذف عکس قدیمی برای جلوگیری از اشغال فضای هارد
                        if (!string.IsNullOrEmpty(existingPost.CoverImagePath))
                        {
                            string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingPost.CoverImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                        }

                        // آپلود عکس جدید
                        existingPost.CoverImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("CoverImagePath", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                _context.Faqs.RemoveRange(existingPost.Faqs);
                if (post.Faqs != null && post.Faqs.Any())
                {
                    foreach (var faq in post.Faqs)
                    {
                        if (!string.IsNullOrWhiteSpace(faq.Question) && !string.IsNullOrWhiteSpace(faq.Answer))
                        {
                            existingPost.Faqs.Add(new Faq { Question = faq.Question, Answer = faq.Answer });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                if (!string.IsNullOrEmpty(post.CoverImagePath))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.CoverImagePath.TrimStart('/'));

                    // بررسی امنیتی برای جلوگیری از حذف فایل‌های خارج از پوشه آپلود
                    if (System.IO.File.Exists(imagePath) && imagePath.Contains("uploads"))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // 🚀 متد اصلاح و ایمن شده برای آپلود داخل ادیتور متنی
        [HttpPost]
        public async Task<IActionResult> UploadArticleImage(IFormFile upload)
        {
            try
            {
                if (upload != null && upload.Length > 0)
                {
                    string url = await ProcessAndSaveImageAsync(upload, "article-images");

                    // فرمت استاندارد CKEditor
                    return Json(new { uploaded = 1, fileName = Path.GetFileName(url), url = url });
                }
                return Json(new { uploaded = 0, error = new { message = "فایلی ارسال نشده است." } });
            }
            catch (Exception ex)
            {
                // مدیریت خطای حجم یا پسوند در ادیتور
                return Json(new { uploaded = 0, error = new { message = ex.Message } });
            }
        }
    }
}