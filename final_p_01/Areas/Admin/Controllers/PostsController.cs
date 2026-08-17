using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostsController> _logger;

        // 🚀 BUG 3: تعریف Magic Bytes برای بررسی واقعی محتوای فایل‌ها
        private static readonly Dictionary<string, byte[][]> _imageSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpg",  new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png",  new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".gif",  new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF header for WebP
        };

        public PostsController(ApplicationDbContext context, ILogger<PostsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // بررسی واقعی تصویر بودن فایل از طریق خواندن بایت‌های اولیه
        private bool IsValidImageSignature(IFormFile file, string extension)
        {
            if (!_imageSignatures.TryGetValue(extension, out byte[][]? signatures)) return false;

            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

            return signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));
        }

        // 🚀 متد اصلی آپلود (ایمن در برابر RCE، Path Traversal و Spoofing)
        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            // 1. بررسی سایز (حداکثر 3 مگابایت)
            const long maxFileSize = 3 * 1024 * 1024;
            if (file.Length > maxFileSize)
                throw new InvalidOperationException("حجم تصویر بیش از حد مجاز است.");

            // 2. بررسی پسوند
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_imageSignatures.ContainsKey(extension))
                throw new InvalidOperationException("فرمت فایل معتبر نیست.");

            // 3. بررسی اولیه MIME Type
            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("فرمت فایل معتبر نیست.");

            // 4. بررسی واقعی محتوای فایل (Magic Bytes)
            if (!IsValidImageSignature(file, extension))
                throw new InvalidOperationException("محتوای فایل دستکاری شده و نامعتبر است.");

            // 5. ایجاد مسیر امن با نام تصادفی
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 6. ذخیره فایل
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        // 🚀 BUG 6: متد کمکی برای حذف کاملاً امن فایل‌ها با Canonicalization
        private void DeleteSecurely(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string allowedDirectory = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

                // پاک‌سازی مسیر ورودی از اسلش‌های اضافی
                string cleanedPath = relativePath.TrimStart('/', '\\');
                string targetPath = Path.GetFullPath(Path.Combine(webRootPath, cleanedPath));

                // فقط در صورتی حذف کن که مسیر نهایی درون پوشه uploads باشد (جلوگیری از ../)
                if (targetPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(targetPath))
                {
                    System.IO.File.Delete(targetPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to securely delete old image at path: {Path}", relativePath);
            }
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
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        post.CoverImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    }
                }
                catch (Exception ex)
                {
                    // 🚀 BUG 4: لاگ کردن استثنا در سرور و نمایش پیام عمومی به کاربر
                    _logger.LogError(ex, "Error uploading cover image during Post Create.");
                    ModelState.AddModelError("CoverImagePath", "خطایی در آپلود تصویر رخ داد. لطفاً تصویر معتبری انتخاب کنید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                post.CreatedAt = DateTime.UtcNow;
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

                string? oldImagePathToDelete = null;

                try
                {
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        // 🚀 BUG 5: آماده‌سازی برای حذف، اما فقط بعد از ذخیره موفق دیتابیس
                        oldImagePathToDelete = existingPost.CoverImagePath;
                        existingPost.CoverImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading cover image during Post Edit. Post ID: {PostId}", existingPost.Id);
                    ModelState.AddModelError("CoverImagePath", "خطایی در آپلود تصویر جدید رخ داد. فایل معتبر نیست.");
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

                // ابتدا در دیتابیس ذخیره می‌کنیم
                await _context.SaveChangesAsync();

                // اگر دیتابیس با موفقیت ثبت شد، حالا با خیال راحت فایل قدیمی را حذف می‌کنیم
                if (!string.IsNullOrEmpty(oldImagePathToDelete))
                {
                    DeleteSecurely(oldImagePathToDelete);
                }

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
                string? imagePathToDelete = post.CoverImagePath;

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                // حذف امنِ عکسِ مقاله پس از موفقیت دیتابیس
                DeleteSecurely(imagePathToDelete);
            }
            return RedirectToAction("Index");
        }

        // 🚀 BUG 7: بازگرداندن فرمت کاملاً سازگار با TinyMCE
        [HttpPost]
        public async Task<IActionResult> UploadArticleImage(IFormFile upload)
        {
            try
            {
                if (upload != null && upload.Length > 0)
                {
                    string url = await ProcessAndSaveImageAsync(upload, "article-images");

                    // فرمت دقیق استاندارد TinyMCE 
                    return Json(new { location = url });
                }

                return BadRequest("فایلی ارسال نشده است.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload image from TinyMCE editor.");
                // ارورهای TinyMCE بهتر است با Status Code 400 بازگردانده شوند
                return BadRequest("خطا در آپلود تصویر. لطفاً تصویر معتبری انتخاب کنید.");
            }
        }
    }
}