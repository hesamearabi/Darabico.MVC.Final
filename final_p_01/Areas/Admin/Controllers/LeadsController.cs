using System.Text;
using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Domain.Enums;
using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using final_p_01.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = "AdminOnly"), Route("admin/leads")]
public class LeadsController(ApplicationDbContext db, IInquiryAttachmentStorage storage) : Controller
{
    private IQueryable<InquiryLead> Query(LeadFilter filter)
    {
        var query = db.InquiryLeads.AsNoTracking();
        if (filter.Status.HasValue) query = query.Where(l => l.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.City)) query = query.Where(l => l.City == filter.City);
        if (!string.IsNullOrWhiteSpace(filter.Source)) query = query.Where(l => (l.Source ?? l.LeadType) == filter.Source);
        if (!string.IsNullOrWhiteSpace(filter.LeadType)) query = query.Where(l => l.LeadType == filter.LeadType);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = DigitNormalizer.Normalize(filter.Search).Trim();
            query = query.Where(l => (l.FullName != null && l.FullName.Contains(search)) || l.Mobile.Contains(search));
        }
        DateTime? from = null, to = null;
        foreach (var (key, value) in new[] { (nameof(filter.From), filter.From), (nameof(filter.To), filter.To) })
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            try { var date = LeadPresentation.ParseDate(value); if (key == nameof(filter.From)) from = date; else to = date.AddDays(1); }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException) { ModelState.AddModelError(key, "تاریخ شمسی معتبر با قالب سال/ماه/روز وارد کنید."); }
        }
        if (from.HasValue && to.HasValue && from >= to) ModelState.AddModelError(nameof(filter.To), "تاریخ پایان باید پس از تاریخ شروع باشد.");
        if (from.HasValue) query = query.Where(l => l.CreatedAtUtc >= from);
        if (to.HasValue) query = query.Where(l => l.CreatedAtUtc < to);
        return ModelState.IsValid ? query : query.Where(l => false);
    }
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] LeadFilter filter, CancellationToken cancellationToken)
    {
        var query = Query(filter);
        var total = await query.CountAsync(cancellationToken);
        filter.Page = Math.Clamp(filter.Page, 1, Math.Max(1, (total + 24) / 25));
        return View(new LeadListViewModel { Filter = filter, Total = total, Leads = await query.OrderByDescending(l => l.CreatedAtUtc).ThenByDescending(l => l.Id).Skip((filter.Page - 1) * 25).Take(25).ToListAsync(cancellationToken) });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var lead = await db.InquiryLeads.AsNoTracking().Include(l => l.Attachments).Include(l => l.Notes).AsSplitQuery().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lead == null ? NotFound() : View(lead);
    }
    [HttpPost("{id:int}/update"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, LeadUpdateViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!model.Status.HasValue || !Enum.IsDefined(model.Status.Value))
            ModelState.AddModelError(nameof(model.Status), "وضعیت پیگیری معتبر نیست.");
        var lead = await db.InquiryLeads.Include(l => l.Attachments).Include(l => l.Notes).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lead == null) return NotFound();
        if (!ModelState.IsValid) return View("Details", lead);
        lead.Status = model.Status!.Value;
        lead.IsHandled = lead.Status != LeadStatus.New;
        lead.AssignedTo = model.AssignedTo?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "وضعیت پیگیری ذخیره شد.";
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost("{id:int}/notes"), ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string? text, CancellationToken cancellationToken)
    {
        var lead = await db.InquiryLeads.Include(l => l.Attachments).Include(l => l.Notes).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lead == null) return NotFound();
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > 2000)
        {
            ModelState.AddModelError("text", "یادداشت باید بین ۱ تا ۲۰۰۰ کاراکتر باشد.");
            ViewData["NoteText"] = text;
            return View("Details", lead);
        }
        var author = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "Admin";
        db.InquiryLeadNotes.Add(new InquiryLeadNote { InquiryLeadId = id, Text = text.Trim(), Author = author[..Math.Min(author.Length, 200)] });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "یادداشت پیگیری ثبت شد.";
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet("attachments/{id:int}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await db.InquiryLeadAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (file == null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        try { return File(storage.Open(file.StorageKey), file.ContentType, file.OriginalFileName); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var lead = await db.InquiryLeads
            .Include(l => l.Attachments)
            .Include(l => l.Notes)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lead == null)
        {
            TempData["Error"] = "درخواست یافت نشد یا قبلاً حذف شده است.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var attachment in lead.Attachments)
        {
            try { storage.Delete(attachment.StorageKey); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete attachment file {attachment.StorageKey}: {ex.Message}");
            }
        }

        db.InquiryLeads.Remove(lead);
        await db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"درخواست «{lead.FullName}» و پیوست‌های آن برای همیشه حذف شد.";
        return RedirectToAction(nameof(Index));
    }
    public static string CsvCell(string? value)
    {
        value ??= "";
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] LeadFilter filter, CancellationToken cancellationToken)
    {
        var query = Query(filter);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=leads.csv";
        Response.Headers.CacheControl = "no-store";
        await Response.WriteAsync("\uFEFFشناسه,تاریخ شمسی (تهران),نام,موبایل,شهر,نوع لید,منبع,وضعیت,مسئول,نوع محل,تعداد زمین,نوع زمین,زمان شروع,بودجه,توضیحات,صفحه فرود,ارجاع,UTM Source,UTM Medium,UTM Campaign\r\n", cancellationToken);
        await foreach (var lead in query.OrderByDescending(l => l.CreatedAtUtc).ThenByDescending(l => l.Id).AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var cells = new[] { lead.Id.ToString(), LeadPresentation.Date(lead.CreatedAtUtc), lead.FullName, lead.Mobile, lead.City, lead.LeadType, lead.Source ?? lead.LeadType, LeadPresentation.Status(lead.Status), lead.AssignedTo, lead.SiteType, lead.CourtCount?.ToString(), lead.CourtType.HasValue && lead.LeadType == "inquiry" ? InquiryFormViewModel.CourtTypes[lead.CourtType.Value] : lead.CourtType?.ToString(), lead.EstimatedStartTime, lead.ApproximateBudget, lead.Message, lead.LandingPage, lead.Referrer, lead.UtmSource, lead.UtmMedium, lead.UtmCampaign };
            await Response.WriteAsync(string.Join(',', cells.Select(CsvCell)) + "\r\n", cancellationToken);
        }
        return new EmptyResult();
    }
}
