using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[OutputCache(NoStore = true)]
public class PhaseModulesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PhaseModulesController> _logger;
    private readonly final_p_01.Services.IRichTextSanitizer _sanitizer;

    public PhaseModulesController(
        ApplicationDbContext context,
        IOutputCacheStore outputCacheStore,
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<PhaseModulesController> logger,
        final_p_01.Services.IRichTextSanitizer sanitizer)
    {
        _context = context;
        _outputCacheStore = outputCacheStore;
        _configuration = configuration;
        _env = env;
        _logger = logger;
        _sanitizer = sanitizer;
    }

    public sealed class PillarFaqInput
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }

    private async Task PurgePublicCacheAsync() =>
        await _outputCacheStore.EvictByTagAsync("BlogCache", default);

    private static string GenerateSeoFriendlySlug(string? text)
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

    private async Task<string> ProcessAndSaveImageAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("فایلی ارسال نشده است.");

        int maxMb = _configuration.GetValue<int>("SiteSettings:MaxImageUploadSizeMB", 3);
        long maxFileSize = maxMb * 1024L * 1024L;

        if (file.Length > maxFileSize)
            throw new InvalidOperationException($"حجم تصویر نباید بیشتر از {maxMb} مگابایت باشد.");

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("نوع محتوای فایل معتبر نیست.");

        string uniqueFileName = $"{Guid.NewGuid()}.webp";
        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

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
            _logger.LogError(ex, "خطا در پردازش تصویر صفحه مادر با ImageSharp");
            throw new InvalidOperationException("محتوای فایل تصویر نامعتبر یا خراب است.");
        }

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }

    private void DeleteSecurely(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

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

    private static List<Faq> MapValidFaqs(List<PillarFaqInput>? faqs, string pageKey)
    {
        if (faqs is null || faqs.Count == 0)
            return new List<Faq>();

        var result = new List<Faq>();
        int sortOrder = 0;

        foreach (var item in faqs)
        {
            if (string.IsNullOrWhiteSpace(item.Question) || string.IsNullOrWhiteSpace(item.Answer))
                continue;

            result.Add(new Faq
            {
                Question = item.Question.Trim(),
                Answer = item.Answer.Trim(),
                PageKey = pageKey,
                SortOrder = sortOrder++
            });
        }

        return result;
    }

    // ---------- Booking slots ----------
    [HttpGet("admin/booking-slots")]
    public async Task<IActionResult> BookingSlots() => View(await ListBookingSlotsAsync());

    [HttpPost("admin/booking-slots/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBookingSlot(BookingSlot model)
    {
        if (model.Date < DateOnly.FromDateTime(DateTime.UtcNow)) ModelState.AddModelError(nameof(model.Date), "تاریخ بازدید نباید در گذشته باشد.");
        if (!TimeOnly.TryParseExact(model.StartTime, "HH:mm", out var start) || !TimeOnly.TryParseExact(model.EndTime, "HH:mm", out var end) || end <= start)
            ModelState.AddModelError(nameof(model.EndTime), "ساعت پایان باید پس از ساعت شروع باشد؛ قالب معتبر 09:00 است.");
        if (model.Capacity < 1) ModelState.AddModelError(nameof(model.Capacity), "ظرفیت باید حداقل یک نفر باشد.");
        if (!ModelState.IsValid) return View("BookingSlots", await ListBookingSlotsAsync());
        _context.BookingSlots.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(BookingSlots));
    }

    [HttpPost("admin/booking-slots/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBookingSlot(int id)
    {
        var slot = await _context.BookingSlots.FindAsync(id);
        if (slot is not null)
        {
            slot.IsActive = !slot.IsActive;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(BookingSlots));
    }

    private Task<List<BookingSlot>> ListBookingSlotsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _context.BookingSlots.AsNoTracking().Where(s => s.Date >= today)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync();
    }

    // ---------- Site settings (video, feasibility page, language toggle) ----------
    [HttpGet("admin/site-settings")]
    public async Task<IActionResult> SiteSettingsForm()
    {
        var settings = await _context.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);
        return View("SiteSettings", settings ?? new SiteSettings { Id = SiteSettings.SingletonId });
    }

    [HttpPost("admin/site-settings"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SiteSettingsForm(SiteSettings model)
    {
        if (!ModelState.IsValid) return View("SiteSettings", model);
        var settings = await _context.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);

        if (settings is null)
        {
            model.Id = SiteSettings.SingletonId;
            _context.SiteSettings.Add(model);
        }
        else
        {
            settings.CanonicalHost = model.CanonicalHost?.Trim() ?? "darabico.com";
            settings.CanonicalUseWww = model.CanonicalUseWww;
            settings.WorkshopVideoUrl = model.WorkshopVideoUrl;
            settings.WorkshopVideoPosterUrl = model.WorkshopVideoPosterUrl;
            settings.WorkshopVideoTitle = model.WorkshopVideoTitle;
            settings.WorkshopVideoDescription = _sanitizer.Sanitize(model.WorkshopVideoDescription);
            settings.FeasibilityPdfPath = model.FeasibilityPdfPath;
            settings.FeasibilityPageTitle = model.FeasibilityPageTitle;
            settings.FeasibilityPageIntro = model.FeasibilityPageIntro;
            settings.FeasibilityMetaTitle = model.FeasibilityMetaTitle;
            settings.FeasibilityMetaDescription = model.FeasibilityMetaDescription;
            settings.PaymentTermsHtml = model.PaymentTermsHtml;
            settings.WarrantyHtml = model.WarrantyHtml;
            settings.AfterSalesIntro = model.AfterSalesIntro;
            settings.EnableMultiLanguage = model.EnableMultiLanguage;
            settings.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        TempData["Success"] = "تنظیمات سایت ذخیره شد.";
        return RedirectToAction(nameof(SiteSettingsForm));
    }

    // ---------- Estimator settings ----------
    [HttpGet("admin/estimator-settings")]
    public async Task<IActionResult> EstimatorSettingsForm()
    {
        var settings = await _context.EstimatorSettings
            .Include(s => s.MaterialTiers)
            .Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);
        return View("EstimatorSettings", settings ?? new EstimatorSettings { Id = EstimatorSettings.SingletonId });
    }

    [HttpPost("admin/estimator-settings"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EstimatorSettingsForm(EstimatorSettings model)
    {
        if (!ModelState.IsValid)
        {
            await LoadEstimatorChildrenAsync(model);
            return View("EstimatorSettings", model);
        }

        var settings = await _context.EstimatorSettings
            .Include(s => s.MaterialTiers)
            .Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);

        if (settings is null)
        {
            model.Id = EstimatorSettings.SingletonId;
            _context.EstimatorSettings.Add(model);
        }
        else
        {
            settings.BasePricePerCourtOutdoor = model.BasePricePerCourtOutdoor;
            settings.BasePricePerCourtIndoor = model.BasePricePerCourtIndoor;
            settings.UpperBoundMultiplier = model.UpperBoundMultiplier;
            settings.Disclaimer = model.Disclaimer;
            settings.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "تنظیمات برآوردگر ذخیره شد.";
        return RedirectToAction(nameof(EstimatorSettingsForm));
    }

    [HttpPost("admin/estimator-tiers/add"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTier(string title, decimal multiplier, int sortOrder)
    {
        var settings = await _context.EstimatorSettings.Include(s => s.MaterialTiers).Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100) ModelState.AddModelError("title", "عنوان متریال باید بین ۱ تا ۱۰۰ کاراکتر باشد.");
        if (multiplier <= 0) ModelState.AddModelError("multiplier", "ضریب باید بزرگ‌تر از صفر باشد.");
        if (settings == null) ModelState.AddModelError(string.Empty, "ابتدا تنظیمات اصلی برآورد هزینه را ذخیره کنید.");
        if (!ModelState.IsValid) return View("EstimatorSettings", settings ?? new EstimatorSettings());
        _context.Set<EstimatorMaterialTier>().Add(new EstimatorMaterialTier
        {
            Title = title.Trim(),
            Multiplier = multiplier,
            SortOrder = sortOrder,
            IsActive = true,
            EstimatorSettingsId = settings!.Id
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(EstimatorSettingsForm));
    }

    [HttpPost("admin/estimator-tiers/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTier(int id)
    {
        var tier = await _context.Set<EstimatorMaterialTier>().FindAsync(id);
        if (tier is not null)
        {
            tier.IsActive = !tier.IsActive;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(EstimatorSettingsForm));
    }

    [HttpPost("admin/estimator-cities/add"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCity(string city, decimal multiplier)
    {
        var settings = await _context.EstimatorSettings.Include(s => s.MaterialTiers).Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);
        if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > 100) ModelState.AddModelError("city", "نام شهر باید بین ۱ تا ۱۰۰ کاراکتر باشد.");
        if (multiplier <= 0) ModelState.AddModelError("multiplier", "ضریب باید بزرگ‌تر از صفر باشد.");
        if (settings == null) ModelState.AddModelError(string.Empty, "ابتدا تنظیمات اصلی برآورد هزینه را ذخیره کنید.");
        if (!ModelState.IsValid) return View("EstimatorSettings", settings ?? new EstimatorSettings());
        _context.Set<EstimatorCityMultiplier>().Add(new EstimatorCityMultiplier
        {
            City = city.Trim(),
            Multiplier = multiplier,
            IsActive = true,
            EstimatorSettingsId = settings!.Id
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(EstimatorSettingsForm));
    }

    [HttpPost("admin/estimator-cities/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCity(int id)
    {
        var city = await _context.Set<EstimatorCityMultiplier>().FindAsync(id);
        if (city is not null)
        {
            city.IsActive = !city.IsActive;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(EstimatorSettingsForm));
    }

    private async Task LoadEstimatorChildrenAsync(EstimatorSettings model)
    {
        model.MaterialTiers = await _context.Set<EstimatorMaterialTier>().AsNoTracking()
            .Where(t => t.EstimatorSettingsId == EstimatorSettings.SingletonId)
            .OrderBy(t => t.SortOrder).ToListAsync();
        model.CityMultipliers = await _context.Set<EstimatorCityMultiplier>().AsNoTracking()
            .Where(c => c.EstimatorSettingsId == EstimatorSettings.SingletonId)
            .OrderBy(c => c.City).ToListAsync();
    }

    // ---------- Pillar pages (صفحات مادر) ----------
    [HttpGet("admin/pillar-pages")]
    public async Task<IActionResult> PillarPages() =>
        View(await _context.PillarPages.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.UpdatedAtUtc)
            .ToListAsync());

    [HttpGet("admin/pillar-pages/create")]
    public IActionResult CreatePillarPage()
    {
        ViewBag.Faqs = new List<Faq>();
        return View(new PillarPage { IsPublished = true });
    }

    [HttpPost("admin/pillar-pages/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePillarPage(
        PillarPage model,
        IFormFile? coverImage,
        List<PillarFaqInput>? faqs)
    {
        ModelState.Remove(nameof(PillarPage.Slug));
        ModelState.Remove(nameof(PillarPage.ContentHtml));

        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        model.ContentHtml ??= string.Empty;
        model.ContentHtml = _sanitizer.Sanitize(model.ContentHtml);
        model.MetaTitle = string.IsNullOrWhiteSpace(model.MetaTitle) ? null : model.MetaTitle.Trim();
        model.MetaDescription = string.IsNullOrWhiteSpace(model.MetaDescription) ? null : model.MetaDescription.Trim();
        model.Canonical = string.IsNullOrWhiteSpace(model.Canonical) ? null : model.Canonical.Trim();

        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "اسلاگ معتبر نیست. لطفاً یک آدرس معتبر وارد کنید.");
        }

        if (ModelState.IsValid && await _context.PillarPages.AnyAsync(p => p.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "این آدرس (Slug) قبلاً برای صفحه مادر دیگری ثبت شده است.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Faqs = MapValidFaqs(faqs, $"pillar:{model.Slug}");
            return View(model);
        }

        string? uploadedImagePath = null;
        try
        {
            if (coverImage != null && coverImage.Length > 0)
            {
                uploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "pillars");
                model.OgImageUrl = uploadedImagePath;
            }

            model.UpdatedAtUtc = DateTime.UtcNow;
            _context.PillarPages.Add(model);

            var validFaqs = MapValidFaqs(faqs, $"pillar:{model.Slug}");
            if (validFaqs.Count > 0)
            {
                _context.Faqs.AddRange(validFaqs);
            }

            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();

            string? metaWarning = GetMetaDescriptionWarning(model.MetaDescription);
            if (!string.IsNullOrEmpty(metaWarning))
            {
                TempData["WarningMessage"] = metaWarning;
            }

            TempData["Success"] = "صفحه مادر با موفقیت ذخیره و منتشر شد.";
            return RedirectToAction(nameof(PillarPages));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Faqs = MapValidFaqs(faqs, $"pillar:{model.Slug}");
            return View(model);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(uploadedImagePath))
                DeleteSecurely(uploadedImagePath);

            _logger.LogError(ex, "System Error during PillarPage Create.");
            ModelState.AddModelError(string.Empty, "خطای داخلی رخ داد. مجدداً تلاش کنید.");
            ViewBag.Faqs = MapValidFaqs(faqs, $"pillar:{model.Slug}");
            return View(model);
        }
    }

    [HttpGet("admin/pillar-pages/edit/{id:int}")]
    public async Task<IActionResult> EditPillarPage(int id)
    {
        var page = await _context.PillarPages.FindAsync(id);
        if (page is null) return NotFound();

        ViewBag.Faqs = await _context.Faqs.AsNoTracking()
            .Where(f => f.PageKey == $"pillar:{page.Slug}")
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        return View("CreatePillarPage", page);
    }

    [HttpPost("admin/pillar-pages/edit/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPillarPage(
        int id,
        PillarPage model,
        IFormFile? coverImage,
        List<PillarFaqInput>? faqs)
    {
        if (id != model.Id) return NotFound();

        var page = await _context.PillarPages.FindAsync(id);
        if (page is null) return NotFound();

        ModelState.Remove(nameof(PillarPage.Slug));
        ModelState.Remove(nameof(PillarPage.ContentHtml));

        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Slug = GenerateSeoFriendlySlug(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        model.ContentHtml ??= string.Empty;
        model.ContentHtml = _sanitizer.Sanitize(model.ContentHtml);
        model.MetaTitle = string.IsNullOrWhiteSpace(model.MetaTitle) ? null : model.MetaTitle.Trim();
        model.MetaDescription = string.IsNullOrWhiteSpace(model.MetaDescription) ? null : model.MetaDescription.Trim();
        model.Canonical = string.IsNullOrWhiteSpace(model.Canonical) ? null : model.Canonical.Trim();

        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "اسلاگ معتبر نیست. لطفاً یک آدرس معتبر وارد کنید.");
        }

        if (ModelState.IsValid && await _context.PillarPages.AnyAsync(p => p.Id != id && p.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "این آدرس (Slug) قبلاً برای صفحه مادر دیگری ثبت شده است.");
        }

        if (!ModelState.IsValid)
        {
            model.OgImageUrl ??= page.OgImageUrl;
            ViewBag.Faqs = MapValidFaqs(faqs, $"pillar:{model.Slug}");
            return View("CreatePillarPage", model);
        }

        string oldPageKey = $"pillar:{page.Slug}";
        string newPageKey = $"pillar:{model.Slug}";
        string? oldImagePathToDelete = null;
        string? newlyUploadedImagePath = null;

        try
        {
            if (coverImage != null && coverImage.Length > 0)
            {
                oldImagePathToDelete = page.OgImageUrl;
                newlyUploadedImagePath = await ProcessAndSaveImageAsync(coverImage, "pillars");
                page.OgImageUrl = newlyUploadedImagePath;
            }
            else
            {
                page.OgImageUrl = string.IsNullOrWhiteSpace(model.OgImageUrl) ? page.OgImageUrl : model.OgImageUrl.Trim();
            }

            page.Title = model.Title;
            page.Slug = model.Slug;
            page.ContentHtml = model.ContentHtml;
            page.MetaTitle = model.MetaTitle;
            page.MetaDescription = model.MetaDescription;
            page.Canonical = model.Canonical;
            page.IsPublished = model.IsPublished;
            page.SortOrder = model.SortOrder;
            page.UpdatedAtUtc = DateTime.UtcNow;

            var existingFaqs = await _context.Faqs
                .Where(f => f.PageKey == oldPageKey || f.PageKey == newPageKey)
                .ToListAsync();
            if (existingFaqs.Count > 0)
            {
                _context.Faqs.RemoveRange(existingFaqs);
            }

            var validFaqs = MapValidFaqs(faqs, newPageKey);
            if (validFaqs.Count > 0)
            {
                _context.Faqs.AddRange(validFaqs);
            }

            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();

            if (!string.IsNullOrEmpty(oldImagePathToDelete))
                DeleteSecurely(oldImagePathToDelete);

            string? metaWarning = GetMetaDescriptionWarning(model.MetaDescription);
            if (!string.IsNullOrEmpty(metaWarning))
            {
                TempData["WarningMessage"] = metaWarning;
            }

            TempData["Success"] = "تغییرات صفحه مادر با موفقیت ذخیره شد.";
            return RedirectToAction(nameof(PillarPages));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.OgImageUrl ??= page.OgImageUrl;
            ViewBag.Faqs = MapValidFaqs(faqs, newPageKey);
            return View("CreatePillarPage", model);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(newlyUploadedImagePath))
                DeleteSecurely(newlyUploadedImagePath);

            _logger.LogError(ex, "Error updating PillarPage ID: {PillarPageId}", id);
            ModelState.AddModelError(string.Empty, "خطایی در ذخیره اطلاعات رخ داد.");
            model.OgImageUrl ??= page.OgImageUrl;
            ViewBag.Faqs = MapValidFaqs(faqs, newPageKey);
            return View("CreatePillarPage", model);
        }
    }

    [HttpPost("admin/pillar-pages/delete/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePillarPage(int id)
    {
        var page = await _context.PillarPages.FindAsync(id);
        if (page is not null)
        {
            string? imagePathToDelete = page.OgImageUrl;
            string pageKey = $"pillar:{page.Slug}";

            var relatedFaqs = await _context.Faqs
                .Where(f => f.PageKey == pageKey)
                .ToListAsync();
            if (relatedFaqs.Count > 0)
            {
                _context.Faqs.RemoveRange(relatedFaqs);
            }

            _context.PillarPages.Remove(page);
            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();

            DeleteSecurely(imagePathToDelete);
            TempData["Success"] = "صفحه مادر و سوالات متداول مرتبط با آن حذف شد.";
        }

        return RedirectToAction(nameof(PillarPages));
    }

    // ---------- City pages ----------
    [HttpGet("admin/city-pages")]
    public async Task<IActionResult> CityPages()
    {
        var pages = await _context.CityPages.AsNoTracking().OrderBy(c => c.CityName).ToListAsync();
        var projectCounts = await _context.Projects.AsNoTracking()
            .Where(p => p.IsPublished)
            .GroupBy(p => p.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.City, x => x.Count);

        ViewData["ProjectCounts"] = projectCounts;
        return View(pages);
    }

    [HttpGet("admin/city-pages/create")] public IActionResult CreateCityPage() => View(new CityPage());

    [HttpPost("admin/city-pages/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCityPage(CityPage model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Slug = model.Slug.Trim();

        // قاعده سخت انتشار: بدون حداقل یک پروژه منتشرشده، اجازه انتشار نیست.
        if (model.IsPublished)
        {
            var publishedProjects = await _context.Projects.CountAsync(p => p.IsPublished && p.City == model.CityName);
            if (publishedProjects < CityPage.MinPublishedProjectsToPublish)
            {
                ModelState.AddModelError(nameof(model.IsPublished),
                    $"انتشار صفحه شهر فقط با حداقل {CityPage.MinPublishedProjectsToPublish} پروژه منتشرشده در «{model.CityName}» امکان‌پذیر است.");
                return View(model);
            }
        }

        if (await _context.CityPages.AnyAsync(c => c.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "این نامک قبلاً استفاده شده است.");
            return View(model);
        }

        model.ContentHtml = _sanitizer.Sanitize(model.ContentHtml);
        _context.CityPages.Add(model);
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(CityPages));
    }

    [HttpPost("admin/city-pages/{id:int}/publish-toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCityPage(int id)
    {
        var page = await _context.CityPages.FindAsync(id);
        if (page is null) return NotFound();

        if (!page.IsPublished)
        {
            var publishedProjects = await _context.Projects.CountAsync(p => p.IsPublished && p.City == page.CityName);
            if (publishedProjects < CityPage.MinPublishedProjectsToPublish)
            {
                TempData["Error"] = $"انتشار فقط با حداقل {CityPage.MinPublishedProjectsToPublish} پروژه منتشرشده در «{page.CityName}» امکان‌پذیر است.";
                return RedirectToAction(nameof(CityPages));
            }
        }

        page.IsPublished = !page.IsPublished;
        page.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(CityPages));
    }

    [NonAction]
    public IActionResult Leads(string? type) => RedirectToAction("Index", "Leads", new { area = "Admin", leadType = type });

    [HttpPost("admin/leads/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLead(int id)
    {
        var lead = await _context.InquiryLeads.FindAsync(id);
        if (lead is not null)
        {
            lead.IsHandled = !lead.IsHandled;
            lead.Status = lead.IsHandled ? final_p_01.Domain.Enums.LeadStatus.Contacted : final_p_01.Domain.Enums.LeadStatus.New;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Leads", new { area = "Admin", leadType = lead?.LeadType });
    }
}