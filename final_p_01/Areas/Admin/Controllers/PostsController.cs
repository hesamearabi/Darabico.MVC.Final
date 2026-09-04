using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostsController> _logger;
        private readonly IConfiguration _configuration;

        public PostsController(ApplicationDbContext context, ILogger<PostsController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // 🚀 SEO Helper: متد استانداردسازی قطعی Slug برای جلوگیری از %20 و حروف بزرگ
        private string GenerateSeoFriendlySlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // 1. تبدیل به حروف کوچک و حذف فاصله‌های ابتدا و انتها
            text = text.ToLowerInvariant().Trim();

            // 2. جایگزینی تمام فاصله‌ها و کاراکترهای جداکننده با خط تیره (-)
            text = Regex.Replace(text, @"\s+", "-");

            // 3. حذف تمام کاراکترهای غیرمجاز (فقط حروف انگلیسی، اعداد، حروف فارسی/عربی و خط تیره مجاز است)
            text = Regex.Replace(text, @"[^a-z0-9\u0600-\u06FF\-]+", "");

            // 4. حذف خط تیره‌های تکراری (مثل ---)
            text = Regex.Replace(text, @"-+", "-");

            return text.Trim('-');
        }

        // 🚀 Performance: متد فشرده‌سازی و تبدیل اجباری تمام تصاویر به فرمت WebP
        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) throw new InvalidOperationException("فایلی ارسال نشده است.");

            int maxMb = _configuration.GetValue<int>("SiteSettings:MaxImageUploadSizeMB", 3);
            long maxFileSize = maxMb * 1024 * 1024;

            if (file.Length > maxFileSize)
                throw new InvalidOperationException($"حجم تصویر نباید بیشتر از {maxMb} مگابایت باشد.");

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("نوع محتوای فایل معتبر نیست.");

            string uniqueFileName = $"{Guid.NewGuid()}.webp"; // 🚀 اجبار به فرمت WebP
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                // 🚀 تنظیم کیفیت فشرده‌سازی روی 80 درصد برای بالاترین سرعت لود بدون افت کیفیت ظاهری
                var encoder = new WebpEncoder { Quality = 80 };

                await image.SaveAsWebpAsync(filePath, encoder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پردازش تصویر با ImageSharp");
                throw new InvalidOperationException("محتوای فایل نامعتبر یا خراب است.");
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        private void DeleteSecurely(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                                            .Replace('\\', Path.DirectorySeparatorChar)
                                                            .TrimStart(Path.DirectorySeparatorChar);

                string targetPath = Path.GetFullPath(Path.Combine(webRootPath, normalizedRelativePath));
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
                // 🚀 SEO: تولید یا اصلاح خودکار Slug از روی عنوان یا اسلاگ وارد شده
                post.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا مشابه آدرس دیگری است.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                string? uploadedImagePath = null;

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
                    ModelState.AddModelError("CoverImagePath", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                catch (Exception ex)
                {
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
                // 🚀 SEO: استانداردسازی اسلاگ در هنگام ویرایش
                post.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.Id != post.Id);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا مشابه آدرس دیگری است.");
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
                string? newlyUploadedImagePath = null;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadArticleImage(IFormFile upload)
        {
            try
            {
                // 🚀 حالا عکس‌هایی که داخل متن TinyMCE قرار می‌گیرند هم WebP و کم‌حجم می‌شوند!
                string url = await ProcessAndSaveImageAsync(upload, "article-images");
                return Json(new { location = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System error during TinyMCE image upload.");
                return BadRequest("خطای سرور در آپلود عکس.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadArticleVideo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("فایلی ارسال نشده است.");

                int maxMb = _configuration.GetValue<int>("SiteSettings:MaxVideoUploadSizeMB", 50);
                long maxFileSize = maxMb * 1024 * 1024;

                if (file.Length > maxFileSize)
                    return BadRequest($"حجم ویدیو نباید بیشتر از {maxMb} مگابایت باشد.");

                var allowedExtString = _configuration.GetValue<string>("SiteSettings:AllowedVideoExtensions", ".mp4,.webm,.ogg");
                var allowedExtensions = allowedExtString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("فرمت ویدیو مجاز نیست.");

                if (!file.ContentType.StartsWith("video/")) return BadRequest("نوع محتوای فایل معتبر نیست.");

                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, 8);

                var signatures = new List<byte[]>
                {
                    new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
                    new byte[] { 0x1A, 0x45, 0xDF, 0xA3 },
                    new byte[] { 0x4F, 0x67, 0x67, 0x53 }
                };

                bool isSignatureValid = signatures.Any(sig => buffer.Take(sig.Length).SequenceEqual(sig));
                if (!isSignatureValid)
                {
                    return BadRequest("ساختار فایل دستکاری شده و نامعتبر است.");
                }

                stream.Position = 0;

                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "article-videos");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
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