using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using final_p_01.Application.Common.Interfaces;
using final_p_01.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

[Route("inquiry")]
public class InquiryController(ApplicationDbContext db, IInquiryAttachmentStorage storage) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? projectRef, CancellationToken cancellationToken)
    {
        if (projectRef.HasValue && !await db.Projects.AnyAsync(p => p.Id == projectRef && p.IsPublished, cancellationToken)) return NotFound();
        return View(new InquiryFormViewModel { ProjectRef = projectRef, Settings = await Settings(cancellationToken) });
    }

    [HttpPost(""), ValidateAntiForgeryToken, EnableRateLimiting("FormRateLimit")]
    [RequestSizeLimit(27 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 27 * 1024 * 1024)]
    public async Task<IActionResult> Index(InquiryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(model.Website)) return BadRequest();
        model.Mobile = DigitNormalizer.Normalize(model.Mobile).Trim();
        model.CourtCount = DigitNormalizer.Normalize(model.CourtCount).Trim();
        model.City = DigitNormalizer.Normalize(model.City).Trim();
        model.FullName = (model.FullName ?? "").Trim();
        model.Description = DigitNormalizer.Normalize(model.Description).Trim();
        ModelState.Clear();
        TryValidateModel(model);
        model.Settings = await Settings(cancellationToken);
        if (!InquiryFormViewModel.SiteTypes.Contains(model.SiteType)) ModelState.AddModelError(nameof(model.SiteType), "نوع محل معتبر نیست.");
        if (!InquiryFormViewModel.CourtTypes.Contains(model.CourtType)) ModelState.AddModelError(nameof(model.CourtType), "نوع زمین معتبر نیست.");
        if (!InquiryFormViewModel.StartTimes.Contains(model.EstimatedStartTime)) ModelState.AddModelError(nameof(model.EstimatedStartTime), "زمان شروع معتبر نیست.");
        var cities = InquiryFormViewModel.Options(model.Settings.InquiryCities);
        if (cities.Length > 0 && !cities.Contains(model.City)) ModelState.AddModelError(nameof(model.City), "شهر را از فهرست انتخاب کنید.");
        if (!string.IsNullOrEmpty(model.ApproximateBudget) && !InquiryFormViewModel.Options(model.Settings.InquiryBudgetRanges).Contains(model.ApproximateBudget))
            ModelState.AddModelError(nameof(model.ApproximateBudget), "بازه بودجه معتبر نیست.");
        if (model.Source is not ("inquiry" or "home" or "projects")) ModelState.AddModelError(nameof(model.Source), "منبع درخواست معتبر نیست.");
        if (model.ProjectRef.HasValue && !await db.Projects.AnyAsync(p => p.Id == model.ProjectRef && p.IsPublished, cancellationToken)) ModelState.AddModelError(nameof(model.ProjectRef), "پروژه معتبر نیست.");
        if (model.Files.Count > 5) ModelState.AddModelError(nameof(model.Files), "حداکثر ۵ فایل مجاز است.");
        if (!ModelState.IsValid) return InvalidSubmission(model);
        var saved = new List<InquiryLeadAttachment>();
        try
        {
            foreach (var file in model.Files) saved.Add(await storage.SaveAsync(file.ToUploadFile(), cancellationToken));
        }
        catch (InvalidDataException ex)
        {
            foreach (var file in saved) storage.Delete(file.StorageKey);
            ModelState.AddModelError(nameof(model.Files), ex.Message);
            return InvalidSubmission(model);
        }
        catch
        {
            foreach (var file in saved) storage.Delete(file.StorageKey);
            throw;
        }
        var lead = new InquiryLead
        {
            LeadType = "inquiry", FullName = model.FullName, Mobile = model.Mobile, City = model.City,
            SiteType = model.SiteType, CourtCount = int.Parse(model.CourtCount), CourtType = (byte)Array.IndexOf(InquiryFormViewModel.CourtTypes, model.CourtType),
            EstimatedStartTime = model.EstimatedStartTime, ApproximateBudget = model.ApproximateBudget,
            Message = model.Description, Source = model.Source, ProjectId = model.ProjectRef, Attachments = saved
        };
        db.InquiryLeads.Add(lead);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { foreach (var file in saved) storage.Delete(file.StorageKey); throw; }
        if (WantsJson) return Json(new { success = true });
        TempData["InquirySubmitted"] = true;
        return RedirectToAction("Index", model.Source == "home" ? "Home" : "Inquiry", new { projectRef = model.ProjectRef });
    }

    private bool WantsJson => Request.GetTypedHeaders().Accept?.Any(value => value.MediaType.Value == "application/json") == true;

    private IActionResult InvalidSubmission(InquiryFormViewModel model) => WantsJson
        ? new JsonResult(new { success = false, errors = ModelState.Where(entry => entry.Value!.Errors.Count > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray()) }) { StatusCode = StatusCodes.Status422UnprocessableEntity }
        : View("Index", model);

    [HttpGet("thanks")]
    public async Task<IActionResult> Thanks(CancellationToken cancellationToken) => View(await Settings(cancellationToken));
    private async Task<SiteSettings> Settings(CancellationToken cancellationToken) => await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, cancellationToken) ?? new SiteSettings();
}
