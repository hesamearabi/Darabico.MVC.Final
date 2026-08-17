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
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostsController> _logger;

        public PostsController(ApplicationDbContext context, ILogger<PostsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 🚀 BUG 1 & 3: بررسی واقعی محتوای تصویر با بالاترین استاندارد (جلوگیری از RCE و Polyglot)
        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) throw new InvalidOperationException("فایلی ارسال نشده است.");

            const long maxFileSize = 3 * 1024 * 1024; // 3MB
            if (file.Length > maxFileSize)
                throw new InvalidOperationException("حجم تصویر نباید بیشتر از 3 مگابایت باشد.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                throw new InvalidOperationException("فرمت فایل معتبر نیست.");

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("نوع محتوای فایل (MIME) معتبر نیست.");

            // باز کردن سربرگ فایل برای بررسی معماری واقعی عکس (Real Image Decode)
            try
            {
                using var stream = file.OpenReadStream();
                var imageInfo = await Image.IdentifyAsync(stream);
                if (imageInfo == null) throw new Exception();
            }
            catch
            {
                throw new InvalidOperationException("محتوای فایل نامعتبر یا خراب است.");
            }

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        // 🚀 BUG 2: استفاده از Canonical Path برای حبس کردن دسترسی فقط به پوشه مجاز (Path Containment)
        private void DeleteSecurely(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                // جایگزینی اسلش‌ها بر اساس سیستم عامل
                string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                                            .Replace('\\', Path.DirectorySeparatorChar)
                                                            .TrimStart(Path.DirectorySeparatorChar);

                string targetPath = Path.GetFullPath(Path.Combine(webRootPath, normalizedRelativePath));

                // ساخت مسیر مطلقِ پوشه آپلود (حتما با کاراکتر جداکننده در انتها تا هکر نتواند فولدر uploads_backup بسازد)
                string allowedDirectory = Path.GetFullPath(Path.Combine(webRootPath, "uploads")) + Path.DirectorySeparatorChar;

                if (targetPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(targetPath))
                {
                    System.IO.File.Delete(targetPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to securely delete old file at path: {Path}", relativePath);
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
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده است.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                string? uploadedImagePath = null; // برای Cleanup

                try
                {
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        uploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                        post.CoverImagePath = uploadedImagePath;
                    }

                    post.CreatedAt = DateTime.UtcNow;
                    _context.Posts.Add(post);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("CoverImagePath", ex.Message); // خطای Validation معتبر
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                catch (Exception ex)
                {
                    // 🚀 BUG 4 & 5: جلوگیری از باقی ماندن فایل یتیم در سرور هنگام خطای دیتابیس
                    if (!string.IsNullOrEmpty(uploadedImagePath)) DeleteSecurely(uploadedImagePath);

                    _logger.LogError(ex, "System Error during Post Create.");
                    ModelState.AddModelError("", "خطای داخلی رخ داد. مجدداً تلاش کنید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.Include(p => p.Faqs).FirstOrDefaultAsync(p => p.Id == id);
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
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده است.");
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
                string? newlyUploadedImagePath = null; // برای Cleanup

                try
                {
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        oldImagePathToDelete = existingPost.CoverImagePath;
                        newlyUploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                        existingPost.CoverImagePath = newlyUploadedImagePath;
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

                    // 🚀 BUG 4: اگر دیتابیس سیو شد، عکس قبلی را پاک کن
                    if (!string.IsNullOrEmpty(oldImagePathToDelete)) DeleteSecurely(oldImagePathToDelete);

                    return RedirectToAction("Index");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("CoverImagePath", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                catch (Exception ex)
                {
                    // 🚀 BUG 5: اگر ذخیره با خطا روبرو شد، عکس آپلود شده‌ی جدید را پاک کن تا فضا اشغال نکند
                    if (!string.IsNullOrEmpty(newlyUploadedImagePath)) DeleteSecurely(newlyUploadedImagePath);

                    _logger.LogError(ex, "Error updating Post ID: {PostId}", existingPost.Id);
                    ModelState.AddModelError("", "خطایی در ذخیره اطلاعات رخ داد.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
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

                DeleteSecurely(imagePathToDelete);
            }
            return RedirectToAction("Index");
        }

        // 🚀 BUG 9 & 10: ایمن شده در برابر CSRF و هماهنگ با TinyMCE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadArticleImage(IFormFile upload)
        {
            try
            {
                string url = await ProcessAndSaveImageAsync(upload, "article-images");
                return Json(new { location = url });
            }
            catch (InvalidOperationException ex)
            {
                // ارورهای Validation
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System error during TinyMCE image upload.");
                return BadRequest("خطای سرور در آپلود عکس.");
            }
        }

        // 🚀 متد جدید برای آپلود ویدیوی مستقیم در ادیتور + CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadArticleVideo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("فایلی ارسال نشده است.");

                const long maxFileSize = 50 * 1024 * 1024; // 50MB
                if (file.Length > maxFileSize) return BadRequest("حجم ویدیو نباید بیشتر از 50 مگابایت باشد.");

                var allowedExtensions = new[] { ".mp4", ".webm", ".ogg" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension)) return BadRequest("فقط فرمت‌های mp4, webm, ogg مجاز هستند.");

                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "article-videos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return Json(new { location = $"/uploads/article-videos/{uniqueFileName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video upload failed.");
                return BadRequest("خطا در آپلود ویدیو.");
            }
        }
    }
}