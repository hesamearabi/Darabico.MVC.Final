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
using Microsoft.AspNetCore.OutputCaching;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [OutputCache(NoStore = true)]
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

        private string GenerateSeoFriendlySlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\u0600-\u06FF\-]+", "");
            text = Regex.Replace(text, @"-+", "-");
            return text.Trim('-');
        }

        private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) throw new InvalidOperationException("فایلی ارسال نشده است.");

            int maxMb = _configuration.GetValue<int>("SiteSettings:MaxImageUploadSizeMB", 3);
            long maxFileSize = maxMb * 1024 * 1024;

            if (file.Length > maxFileSize)
                throw new InvalidOperationException($"حجم تصویر نباید بیشتر از {maxMb} مگابایت باشد.");

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("نوع محتوای فایل معتبر نیست.");

            string uniqueFileName = $"{Guid.NewGuid()}.webp";
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

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

        // 🚀 فیچر ذخیره فایل صوتی (پادکست)
        private async Task<string> ProcessAndSaveAudioAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            int maxMb = _configuration.GetValue<int>("SiteSettings:MaxAudioUploadSizeMB", 20);
            long maxFileSize = maxMb * 1024 * 1024;

            if (file.Length > maxFileSize)
                throw new InvalidOperationException($"حجم فایل صوتی نباید بیشتر از {maxMb} مگابایت باشد.");

            if (!file.ContentType.StartsWith("audio/"))
                throw new InvalidOperationException("نوع محتوای فایل معتبر نیست. لطفاً فقط فایل صوتی آپلود کنید.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".mp3", ".ogg", ".wav" };

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("فرمت صوتی مجاز نیست (فقط MP3, OGG, WAV).");

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", "audios");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/audios/{uniqueFileName}";
        }

        // 🚀 فیچر ذخیره فایل ضمیمه (PDF, ZIP, RAR)
        private async Task<string> ProcessAndSaveAttachmentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            int maxMb = _configuration.GetValue<int>("SiteSettings:MaxAttachmentUploadSizeMB", 50);
            long maxFileSize = maxMb * 1024 * 1024;

            if (file.Length > maxFileSize)
                throw new InvalidOperationException($"حجم فایل ضمیمه نباید بیشتر از {maxMb} مگابایت باشد.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".zip", ".rar", ".doc", ".docx" };

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("فرمت فایل ضمیمه مجاز نیست (فقط PDF, ZIP, RAR, DOCX).");

            string uniqueFileName = $"{Guid.NewGuid()}{extension}";
            string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", "attachments");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/attachments/{uniqueFileName}";
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
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

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
        public async Task<IActionResult> Create(Post post, IFormFile? coverImage, IFormFile? audioFile, IFormFile? attachmentFile)
        {
            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("AudioFilePath");
            ModelState.Remove("AttachmentFilePath");
            ModelState.Remove("Faqs");
            ModelState.Remove("Slug");

            post.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

            if (ModelState.IsValid)
            {
                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا مشابه آدرس دیگری است.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                string? uploadedImagePath = null;
                string? uploadedAudioPath = null;
                string? uploadedAttachmentPath = null;

                try
                {
                    // آپلود تصویر شاخص
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        uploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                        post.CoverImagePath = uploadedImagePath;
                    }

                    // آپلود فایل صوتی
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        uploadedAudioPath = await ProcessAndSaveAudioAsync(audioFile);
                        post.AudioFilePath = uploadedAudioPath;
                    }

                    // آپلود فایل ضمیمه
                    if (attachmentFile != null && attachmentFile.Length > 0)
                    {
                        uploadedAttachmentPath = await ProcessAndSaveAttachmentAsync(attachmentFile);
                        post.AttachmentFilePath = uploadedAttachmentPath;
                    }

                    // 🚀 ذخیره مقادیر جدید UI/UX برای دانلود
                    post.AttachmentTitle = post.AttachmentTitle;
                    post.AttachmentDescription = post.AttachmentDescription;

                    // تنظیم مقادیر پیش‌فرض برای سیستم امتیازدهی سئو
                    post.AverageRating = 5.0;
                    post.TotalVotes = 1;

                    post.CreatedAt = DateTime.UtcNow;
                    _context.Posts.Add(post);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "مقاله با موفقیت ذخیره و منتشر شد.";
                    return RedirectToAction("Index", new { t = DateTime.UtcNow.Ticks });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                catch (Exception ex)
                {
                    // در صورت خطای دیتابیس، فایل‌هایی که آپلود شده‌اند را پاک می‌کنیم
                    if (!string.IsNullOrEmpty(uploadedImagePath)) DeleteSecurely(uploadedImagePath);
                    if (!string.IsNullOrEmpty(uploadedAudioPath)) DeleteSecurely(uploadedAudioPath);
                    if (!string.IsNullOrEmpty(uploadedAttachmentPath)) DeleteSecurely(uploadedAttachmentPath);

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
        public async Task<IActionResult> Edit(int id, Post post, IFormFile? coverImage, IFormFile? audioFile, IFormFile? attachmentFile)
        {
            if (id != post.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("AudioFilePath");
            ModelState.Remove("AttachmentFilePath");
            ModelState.Remove("Faqs");
            ModelState.Remove("Slug");

            post.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

            if (ModelState.IsValid)
            {
                bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.Id != post.Id);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا مشابه آدرس دیگری است.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                var existingPost = await _context.Posts.Include(p => p.Faqs).FirstOrDefaultAsync(p => p.Id == id);
                if (existingPost == null) return NotFound();

                // آپدیت فیلدهای متنی
                existingPost.Title = post.Title;
                existingPost.Slug = post.Slug;
                existingPost.Content = post.Content;
                existingPost.CategoryId = post.CategoryId;
                existingPost.MetaDescription = post.MetaDescription;
                existingPost.IsPublished = post.IsPublished;
                existingPost.CoverImageAlt = post.CoverImageAlt;

                // 🚀 آپدیت ویژگی‌های جدید فایل ضمیمه
                existingPost.AttachmentTitle = post.AttachmentTitle;
                existingPost.AttachmentDescription = post.AttachmentDescription;

                string? oldImagePathToDelete = null;
                string? newlyUploadedImagePath = null;

                string? oldAudioPathToDelete = null;
                string? newlyUploadedAudioPath = null;

                string? oldAttachmentPathToDelete = null;
                string? newlyUploadedAttachmentPath = null;

                try
                {
                    // آپلود تصویر شاخص جدید
                    if (coverImage != null && coverImage.Length > 0)
                    {
                        oldImagePathToDelete = existingPost.CoverImagePath;
                        newlyUploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                        existingPost.CoverImagePath = newlyUploadedImagePath;
                    }

                    // آپلود فایل صوتی جدید
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        oldAudioPathToDelete = existingPost.AudioFilePath;
                        newlyUploadedAudioPath = await ProcessAndSaveAudioAsync(audioFile);
                        existingPost.AudioFilePath = newlyUploadedAudioPath;
                    }

                    // آپلود فایل ضمیمه جدید
                    if (attachmentFile != null && attachmentFile.Length > 0)
                    {
                        oldAttachmentPathToDelete = existingPost.AttachmentFilePath;
                        newlyUploadedAttachmentPath = await ProcessAndSaveAttachmentAsync(attachmentFile);
                        existingPost.AttachmentFilePath = newlyUploadedAttachmentPath;
                    }

                    // آپدیت سوالات متداول
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

                    // حذف امن فایل‌های قدیمی
                    if (!string.IsNullOrEmpty(oldImagePathToDelete)) DeleteSecurely(oldImagePathToDelete);
                    if (!string.IsNullOrEmpty(oldAudioPathToDelete)) DeleteSecurely(oldAudioPathToDelete);
                    if (!string.IsNullOrEmpty(oldAttachmentPathToDelete)) DeleteSecurely(oldAttachmentPathToDelete);

                    TempData["SuccessMessage"] = "تغییرات مقاله با موفقیت ذخیره شد.";
                    return RedirectToAction("Index", new { t = DateTime.UtcNow.Ticks });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(newlyUploadedImagePath)) DeleteSecurely(newlyUploadedImagePath);
                    if (!string.IsNullOrEmpty(newlyUploadedAudioPath)) DeleteSecurely(newlyUploadedAudioPath);
                    if (!string.IsNullOrEmpty(newlyUploadedAttachmentPath)) DeleteSecurely(newlyUploadedAttachmentPath);

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
                string? audioPathToDelete = post.AudioFilePath;
                string? attachmentPathToDelete = post.AttachmentFilePath;

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                // پاک کردن تمام فایل‌های وابسته هنگام حذف مقاله
                DeleteSecurely(imagePathToDelete);
                DeleteSecurely(audioPathToDelete);
                DeleteSecurely(attachmentPathToDelete);

                TempData["SuccessMessage"] = "مقاله و فایل‌های مرتبط به طور کامل حذف شد.";
            }
            return RedirectToAction("Index", new { t = DateTime.UtcNow.Ticks });
        }

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