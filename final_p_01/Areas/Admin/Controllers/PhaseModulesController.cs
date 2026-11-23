using System.ComponentModel.DataAnnotations;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[OutputCache(NoStore = true)]
public class PhaseModulesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IOutputCacheStore _outputCacheStore;

    public PhaseModulesController(ApplicationDbContext context, IOutputCacheStore outputCacheStore)
    {
        _context = context;
        _outputCacheStore = outputCacheStore;
    }

    private async Task PurgePublicCacheAsync() =>
        await _outputCacheStore.EvictByTagAsync("BlogCache", default);

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
            settings.WorkshopVideoDescription = model.WorkshopVideoDescription;
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
            Title = title.Trim(), Multiplier = multiplier, SortOrder = sortOrder,
            IsActive = true, EstimatorSettingsId = settings!.Id
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
            City = city.Trim(), Multiplier = multiplier, IsActive = true, EstimatorSettingsId = settings!.Id
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

    // ---------- Pillar pages ----------
    [HttpGet("admin/pillar-pages")]
    public async Task<IActionResult> PillarPages() =>
        View(await _context.PillarPages.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync());

    [HttpGet("admin/pillar-pages/create")] public IActionResult CreatePillarPage() => View(new PillarPage());

    [HttpPost("admin/pillar-pages/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePillarPage(PillarPage model)
    {
        if (!ModelState.IsValid) return View(model);

        model.Slug = model.Slug.Trim();
        if (await _context.PillarPages.AnyAsync(p => p.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "این نامک قبلاً استفاده شده است.");
            return View(model);
        }
        _context.PillarPages.Add(model);
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(PillarPages));
    }

    [HttpGet("admin/pillar-pages/edit/{id:int}")]
    public async Task<IActionResult> EditPillarPage(int id)
    {
        var page = await _context.PillarPages.FindAsync(id);
        return page is null ? NotFound() : View("CreatePillarPage", page);
    }

    [HttpPost("admin/pillar-pages/edit/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPillarPage(int id, PillarPage model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View("CreatePillarPage", model);
        model.Slug = model.Slug.Trim();
        if (await _context.PillarPages.AnyAsync(p => p.Id != id && p.Slug == model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "این نامک قبلاً استفاده شده است.");
            return View("CreatePillarPage", model);
        }

        var page = await _context.PillarPages.FindAsync(id);
        if (page is null) return NotFound();

        page.Title = model.Title;
        page.Slug = model.Slug.Trim();
        page.ContentHtml = model.ContentHtml;
        page.MetaTitle = model.MetaTitle;
        page.MetaDescription = model.MetaDescription;
        page.Canonical = model.Canonical;
        page.OgImageUrl = model.OgImageUrl;
        page.IsPublished = model.IsPublished;
        page.SortOrder = model.SortOrder;
        page.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
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

    // ---------- Leads inbox ----------
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
