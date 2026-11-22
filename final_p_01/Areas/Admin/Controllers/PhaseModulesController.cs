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

    // ---------- Site settings (video, feasibility page, process texts, language toggle) ----------
    [HttpGet("admin/site-settings")]
    public async Task<IActionResult> SiteSettingsForm()
    {
        var settings = await _context.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);
        return View(settings ?? new SiteSettings { Id = SiteSettings.SingletonId });
    }

    [HttpPost("admin/site-settings"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SiteSettingsForm(SiteSettings model)
    {
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
        return View(settings ?? new EstimatorSettings { Id = EstimatorSettings.SingletonId });
    }

    [HttpPost("admin/estimator-settings"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EstimatorSettingsForm(EstimatorSettings model)
    {
        if (!ModelState.IsValid)
        {
            await LoadEstimatorChildrenAsync(model);
            return View(model);
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
        if (!string.IsNullOrWhiteSpace(title))
        {
            var settingsId = await _context.EstimatorSettings
                .Where(s => s.Id == EstimatorSettings.SingletonId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
            if (settingsId > 0)
            {
                _context.Set<EstimatorMaterialTier>().Add(new EstimatorMaterialTier
                {
                    Title = title.Trim(),
                    Multiplier = multiplier <= 0 ? 1 : multiplier,
                    SortOrder = sortOrder,
                    IsActive = true,
                    EstimatorSettingsId = settingsId
                });
                await _context.SaveChangesAsync();
            }
        }
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
        if (!string.IsNullOrWhiteSpace(city))
        {
            var settingsId = await _context.EstimatorSettings
                .Where(s => s.Id == EstimatorSettings.SingletonId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
            if (settingsId > 0)
            {
                _context.Set<EstimatorCityMultiplier>().Add(new EstimatorCityMultiplier
                {
                    City = city.Trim(),
                    Multiplier = multiplier <= 0 ? 1 : multiplier,
                    IsActive = true,
                    EstimatorSettingsId = settingsId
                });
                await _context.SaveChangesAsync();
            }
        }
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
        return page is null ? NotFound() : View(page);
    }

    [HttpPost("admin/pillar-pages/edit/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPillarPage(int id, PillarPage model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

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

    // ---------- Process stages ----------
    [HttpGet("admin/process-stages")]
    public async Task<IActionResult> ProcessStages() =>
        View(await _context.ProcessStages.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync());

    [HttpPost("admin/process-stages/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProcessStage(ProcessStage model)
    {
        if (!ModelState.IsValid)
            return View("ProcessStages", await _context.ProcessStages.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync());

        _context.ProcessStages.Add(model);
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(ProcessStages));
    }

    [HttpPost("admin/process-stages/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProcessStage(int id)
    {
        var stage = await _context.ProcessStages.FindAsync(id);
        if (stage is not null)
        {
            stage.IsActive = !stage.IsActive;
            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();
        }
        return RedirectToAction(nameof(ProcessStages));
    }

    // ---------- Documents ----------
    [HttpGet("admin/documents")]
    public async Task<IActionResult> Documents() =>
        View(await _context.DocumentItems.AsNoTracking().OrderBy(d => d.SortOrder).ToListAsync());

    [HttpPost("admin/documents/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDocument(DocumentItem model)
    {
        if (!ModelState.IsValid)
            return View("Documents", await _context.DocumentItems.AsNoTracking().OrderBy(d => d.SortOrder).ToListAsync());

        _context.DocumentItems.Add(model);
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(Documents));
    }

    [HttpPost("admin/documents/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDocument(int id)
    {
        var document = await _context.DocumentItems.FindAsync(id);
        if (document is not null)
        {
            document.IsPublished = !document.IsPublished;
            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();
        }
        return RedirectToAction(nameof(Documents));
    }

    // ---------- Booking slots ----------
    [HttpGet("admin/booking-slots")]
    public async Task<IActionResult> BookingSlots()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return View(await _context.BookingSlots.AsNoTracking()
            .Where(s => s.Date >= today)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync());
    }

    [HttpPost("admin/booking-slots/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBookingSlot(BookingSlot model)
    {
        if (!ModelState.IsValid)
            return View("BookingSlots", await ListBookingSlotsAsync());

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

    private async Task<List<BookingSlot>> ListBookingSlotsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.BookingSlots.AsNoTracking()
            .Where(s => s.Date >= today)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync();
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

    // ---------- FAQ manager ----------
    [HttpGet("admin/faqs")]
    public async Task<IActionResult> Faqs() =>
        View(await _context.Set<Faq>().AsNoTracking().OrderBy(f => f.PageKey).ThenBy(f => f.SortOrder).ToListAsync());

    [HttpPost("admin/faqs/create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaq(Faq model, string? pageKey)
    {
        if (!ModelState.IsValid)
            return View("Faqs", await ListFaqsAsync());

        model.PageKey = string.IsNullOrWhiteSpace(pageKey) ? null : pageKey.Trim();
        if (model.PostId == 0) model.PostId = null;
        _context.Set<Faq>().Add(model);
        await _context.SaveChangesAsync();
        await PurgePublicCacheAsync();
        return RedirectToAction(nameof(Faqs));
    }

    [HttpPost("admin/faqs/{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFaq(int id)
    {
        var faq = await _context.Set<Faq>().FindAsync(id);
        if (faq is not null)
        {
            _context.Set<Faq>().Remove(faq);
            await _context.SaveChangesAsync();
            await PurgePublicCacheAsync();
        }
        return RedirectToAction(nameof(Faqs));
    }

    private async Task<List<Faq>> ListFaqsAsync() =>
        await _context.Set<Faq>().AsNoTracking().OrderBy(f => f.PageKey).ThenBy(f => f.SortOrder).ToListAsync();

    // ---------- Leads inbox ----------
    [HttpGet("admin/leads")]
    public async Task<IActionResult> Leads(string? type)
    {
        var query = _context.InquiryLeads.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(l => l.LeadType == type);

        var leads = await query.OrderByDescending(l => l.CreatedAtUtc).Take(500).ToListAsync();
        ViewData["TypeFilter"] = type;
        return View(leads);
    }

    [HttpPost("admin/leads/{id:int}/toggle"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLead(int id)
    {
        var lead = await _context.InquiryLeads.FindAsync(id);
        if (lead is not null)
        {
            lead.IsHandled = !lead.IsHandled;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Leads), new { type = lead?.LeadType });
    }
}
