using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using final_p_01.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "AdminOnly"), Route("admin/inquiry-settings")]
public class InquirySettingsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var s = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1) ?? new SiteSettings();
        return View(new InquirySettingsViewModel { ThanksTitle = s.InquiryThanksTitle, ThanksBody = s.InquiryThanksBody, Cities = s.InquiryCities, BudgetRanges = s.InquiryBudgetRanges, NotificationEmail = s.LeadNotificationEmail, NotificationPhone = s.LeadNotificationPhone });
    }
    [HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(InquirySettingsViewModel model)
    {
        model.NotificationPhone = string.IsNullOrWhiteSpace(model.NotificationPhone) ? null : DigitNormalizer.Normalize(model.NotificationPhone).Trim();
        ModelState.Clear();
        TryValidateModel(model);
        if (InquiryFormViewModel.Options(model.Cities).Any(s => s.Length > 100) || InquiryFormViewModel.Options(model.BudgetRanges).Any(s => s.Length > 150)) ModelState.AddModelError("", "نام شهر حداکثر ۱۰۰ و بازه بودجه حداکثر ۱۵۰ کاراکتر باشد.");
        if (!ModelState.IsValid) return View(model);
        var s = await db.SiteSettings.FirstOrDefaultAsync(s => s.Id == 1);
        if (s == null) { s = new SiteSettings { Id = 1 }; db.SiteSettings.Add(s); }
        s.InquiryThanksTitle = model.ThanksTitle.Trim(); s.InquiryThanksBody = model.ThanksBody.Trim();
        s.InquiryCities = string.Join('\n', InquiryFormViewModel.Options(model.Cities));
        s.InquiryBudgetRanges = string.Join('\n', InquiryFormViewModel.Options(model.BudgetRanges));
        s.LeadNotificationEmail = model.NotificationEmail?.Trim(); s.LeadNotificationPhone = model.NotificationPhone;
        await db.SaveChangesAsync();
        TempData["InquirySaved"] = "تنظیمات ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }
}
