using System.Text.RegularExpressions;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using Microsoft.AspNetCore.OutputCaching;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[OutputCache(NoStore = true)]
public class PostsController : Controller
{
    private const string GuideSlug = "padel-court-construction-guide";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IOutputCacheStore _outputCacheStore;

    public PostsController(
        ApplicationDbContext context,
        ILogger<PostsController> logger,
        IConfiguration configuration,
        IWebHostEnvironment env,
        IOutputCacheStore outputCacheStore)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _env = env;
        _outputCacheStore = outputCacheStore;
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

    private static string? GetMetaDescriptionWarning(string? metaDescription)
    {
        if (string.IsNullOrWhiteSpace(metaDescription))
        {
            return "هشدار سئو: توضیحات متا (Meta Description) خالی است. توصیه می‌شود متنی اختصاصی بین ۷۰ تا ۱۶۰ کاراکتر وارد کنید.";
        }

        if (metaDescription.Length > 160)
        {
            return $"هشدار سئو: طول توضیحات متا ({metaDescription.Length} کاراکتر) بیشتر از ۱۶۰ کاراکتر است و ممکن است در نتایج گوگل بریده شود.";
        }

        return null;
    }

    private void ApplyMetaDescriptionWarningToModelState(string? metaDescription)
    {
        string? warning = GetMetaDescriptionWarning(metaDescription);
        if (!string.IsNullOrEmpty(warning))
        {
            ModelState.AddModelError(nameof(Post.MetaDescription), warning);
        }
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

        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);

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

        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "audios");

        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/audios/{uniqueFileName}";
    }

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

        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "attachments");

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
            string webRootPath = _env.WebRootPath;
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
        ModelState.Remove("SlugHistories");
        ModelState.Remove("Slug");

        post.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

        if (string.IsNullOrWhiteSpace(post.Slug))
        {
            ModelState.AddModelError("Slug", "اسلاگ معتبر نیست. لطفاً یک آدرس معتبر وارد کنید.");
        }

        if (ModelState.IsValid)
        {
            bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug) ||
                               await _context.PostSlugHistories.AnyAsync(h => h.Slug == post.Slug);
            if (isSlugExist)
            {
                ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا در تاریخچه ریدایرکت‌ها استفاده شده است.");
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                return View(post);
            }

            string? uploadedImagePath = null;
            string? uploadedAudioPath = null;
            string? uploadedAttachmentPath = null;

            try
            {
                if (coverImage != null && coverImage.Length > 0)
                {
                    uploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    post.CoverImagePath = uploadedImagePath;
                }

                if (audioFile != null && audioFile.Length > 0)
                {
                    uploadedAudioPath = await ProcessAndSaveAudioAsync(audioFile);
                    post.AudioFilePath = uploadedAudioPath;
                }

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    uploadedAttachmentPath = await ProcessAndSaveAttachmentAsync(attachmentFile);
                    post.AttachmentFilePath = uploadedAttachmentPath;
                }

                post.AttachmentTitle = post.AttachmentTitle;
                post.AttachmentDescription = post.AttachmentDescription;

                post.AverageRating = 0.0;
                post.TotalVotes = 0;

                post.CreatedAt = DateTime.UtcNow;
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                await _outputCacheStore.EvictByTagAsync("BlogCache", default);

                string? metaWarning = GetMetaDescriptionWarning(post.MetaDescription);
                if (!string.IsNullOrEmpty(metaWarning))
                {
                    TempData["WarningMessage"] = metaWarning;
                    TempData["MetaDescriptionWarning"] = metaWarning;
                }

                TempData["SuccessMessage"] = "مقاله با موفقیت ذخیره و منتشر شد.";
                return RedirectToAction("Index", new { t = DateTime.UtcNow.Ticks });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                return View(post);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(uploadedImagePath)) DeleteSecurely(uploadedImagePath);
                if (!string.IsNullOrEmpty(uploadedAudioPath)) DeleteSecurely(uploadedAudioPath);
                if (!string.IsNullOrEmpty(uploadedAttachmentPath)) DeleteSecurely(uploadedAttachmentPath);

                _logger.LogError(ex, "System Error during Post Create.");
                ModelState.AddModelError("", "خطای داخلی رخ داد. مجدداً تلاش کنید.");
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                return View(post);
            }
        }

        ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
        ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
        return View(post);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _context.Posts.Include(p => p.Faqs).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return NotFound();

        ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
        ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
        return View(post);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Post post, IFormFile? coverImage, IFormFile? audioFile, IFormFile? attachmentFile)
    {
        if (id != post.Id) return NotFound();

        var existingPost = await _context.Posts
            .Include(p => p.Faqs)
            .Include(p => p.SlugHistories)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (existingPost == null) return NotFound();

        ModelState.Remove("Category");
        ModelState.Remove("CoverImagePath");
        ModelState.Remove("AudioFilePath");
        ModelState.Remove("AttachmentFilePath");
        ModelState.Remove("Faqs");
        ModelState.Remove("SlugHistories");
        ModelState.Remove("Slug");

        bool isGuidePost = string.Equals(existingPost.Slug, GuideSlug, StringComparison.OrdinalIgnoreCase);

        string normalizedIncomingSlug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(post.Slug) ? post.Title : post.Slug);

        if (isGuidePost)
        {
            if (!string.Equals(normalizedIncomingSlug, GuideSlug, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Slug", "اسلاگ صفحه راهنمای جامع ساخت زمین پدل محافظت‌شده است و قابل تغییر نیست.");
            }
            post.Slug = GuideSlug;
        }
        else
        {
            post.Slug = normalizedIncomingSlug;

            if (string.IsNullOrWhiteSpace(post.Slug))
            {
                ModelState.AddModelError("Slug", "اسلاگ معتبر نیست. لطفاً یک آدرس معتبر وارد کنید.");
            }
            else if (string.Equals(post.Slug, GuideSlug, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Slug", "این اسلاگ برای صفحه راهنمای جامع رزرو شده است.");
            }
        }

        if (ModelState.IsValid)
        {
            bool isSlugExist = await _context.Posts.AnyAsync(p => p.Slug == post.Slug && p.Id != post.Id) ||
                               await _context.PostSlugHistories.AnyAsync(h => h.Slug == post.Slug && h.PostId != post.Id);
            if (isSlugExist)
            {
                ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً ثبت شده یا در تاریخچه ریدایرکت مقاله دیگری استفاده شده است.");
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                return View(post);
            }

            string previousSlug = existingPost.Slug.Trim().ToLowerInvariant();
            string nextSlug = post.Slug.Trim().ToLowerInvariant();
            bool slugChanged = !string.Equals(previousSlug, nextSlug, StringComparison.OrdinalIgnoreCase);
            bool wasPublished = existingPost.IsPublished;

            bool hasMetadataOrContentChanges =
                existingPost.Title != post.Title ||
                slugChanged ||
                existingPost.Content != post.Content ||
                existingPost.CategoryId != post.CategoryId ||
                (existingPost.MetaDescription ?? string.Empty) != (post.MetaDescription ?? string.Empty) ||
                existingPost.IsPublished != post.IsPublished ||
                (existingPost.CoverImageAlt ?? string.Empty) != (post.CoverImageAlt ?? string.Empty) ||
                (existingPost.AttachmentTitle ?? string.Empty) != (post.AttachmentTitle ?? string.Empty) ||
                (existingPost.AttachmentDescription ?? string.Empty) != (post.AttachmentDescription ?? string.Empty);

            var existingFaqs = existingPost.Faqs
                .Select(f => (f.Question, f.Answer))
                .ToList();

            var incomingFaqs = (post.Faqs ?? Enumerable.Empty<Faq>())
                .Where(f => !string.IsNullOrWhiteSpace(f.Question) && !string.IsNullOrWhiteSpace(f.Answer))
                .Select(f => (f.Question, f.Answer))
                .ToList();

            bool hasFaqChanges = !existingFaqs.SequenceEqual(incomingFaqs);

            if (slugChanged)
            {
                var matchingOldHistory = existingPost.SlugHistories
                    .FirstOrDefault(h => string.Equals(h.Slug, nextSlug, StringComparison.OrdinalIgnoreCase));
                if (matchingOldHistory != null)
                {
                    _context.PostSlugHistories.Remove(matchingOldHistory);
                }

                if (wasPublished && !string.IsNullOrWhiteSpace(previousSlug))
                {
                    bool alreadyInHistory = existingPost.SlugHistories
                        .Any(h => string.Equals(h.Slug, previousSlug, StringComparison.OrdinalIgnoreCase));
                    if (!alreadyInHistory)
                    {
                        existingPost.SlugHistories.Add(new PostSlugHistory
                        {
                            PostId = existingPost.Id,
                            Slug = previousSlug,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            existingPost.Title = post.Title;
            existingPost.Slug = nextSlug;
            existingPost.Content = post.Content;
            existingPost.CategoryId = post.CategoryId;
            existingPost.MetaDescription = post.MetaDescription;
            existingPost.IsPublished = post.IsPublished;
            existingPost.CoverImageAlt = post.CoverImageAlt;

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
                if (coverImage != null && coverImage.Length > 0)
                {
                    oldImagePathToDelete = existingPost.CoverImagePath;
                    newlyUploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "posts");
                    existingPost.CoverImagePath = newlyUploadedImagePath;
                }

                if (audioFile != null && audioFile.Length > 0)
                {
                    oldAudioPathToDelete = existingPost.AudioFilePath;
                    newlyUploadedAudioPath = await ProcessAndSaveAudioAsync(audioFile);
                    existingPost.AudioFilePath = newlyUploadedAudioPath;
                }

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    oldAttachmentPathToDelete = existingPost.AttachmentFilePath;
                    newlyUploadedAttachmentPath = await ProcessAndSaveAttachmentAsync(attachmentFile);
                    existingPost.AttachmentFilePath = newlyUploadedAttachmentPath;
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

                bool hasMediaChanges =
                    newlyUploadedImagePath != null ||
                    newlyUploadedAudioPath != null ||
                    newlyUploadedAttachmentPath != null;

                if (hasMetadataOrContentChanges || hasMediaChanges || hasFaqChanges)
                {
                    existingPost.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _outputCacheStore.EvictByTagAsync("BlogCache", default);

                if (!string.IsNullOrEmpty(oldImagePathToDelete)) DeleteSecurely(oldImagePathToDelete);
                if (!string.IsNullOrEmpty(oldAudioPathToDelete)) DeleteSecurely(oldAudioPathToDelete);
                if (!string.IsNullOrEmpty(oldAttachmentPathToDelete)) DeleteSecurely(oldAttachmentPathToDelete);

                string? metaWarning = GetMetaDescriptionWarning(post.MetaDescription);
                if (!string.IsNullOrEmpty(metaWarning))
                {
                    TempData["WarningMessage"] = metaWarning;
                    TempData["MetaDescriptionWarning"] = metaWarning;
                }

                TempData["SuccessMessage"] = slugChanged && wasPublished
                    ? "تغییرات مقاله ذخیره شد و ریدایرکت 301 از آدرس قبلی به آدرس جدید به صورت خودکار ثبت گردید."
                    : "تغییرات مقاله با موفقیت ذخیره شد.";
                return RedirectToAction("Index", new { t = DateTime.UtcNow.Ticks });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
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
                ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                return View(post);
            }
        }

        ApplyMetaDescriptionWarningToModelState(post.MetaDescription);
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

            await _outputCacheStore.EvictByTagAsync("BlogCache", default);

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

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "article-videos");
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