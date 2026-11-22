using System.ComponentModel.DataAnnotations;
using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

public class BookingFormViewModel
{
    [Display(Name = "اسلات بازدید")]
    public int SlotId { get; set; }

    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است."), MaxLength(150)]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است."), MaxLength(20)]
    [Display(Name = "شماره موبایل")]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "توضیحات")]
    public string? Message { get; set; }

    /// <summary>تله ضداسپم</summary>
    [StringLength(0)]
    public string? Website { get; set; }

    public List<BookingSlot> AvailableSlots { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

[Route("booking")]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(ApplicationDbContext context, ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await BuildFormAsync(new BookingFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("FormRateLimit")]
    public async Task<IActionResult> Index([Bind(nameof(BookingFormViewModel.SlotId), nameof(BookingFormViewModel.FullName), nameof(BookingFormViewModel.Mobile), nameof(BookingFormViewModel.Message), nameof(BookingFormViewModel.Website))] BookingFormViewModel model)
    {
        if (!string.IsNullOrEmpty(model.Website))
            return base.NotFound();

        var mobile = PhoneNormalizer.NormalizeTo09(model.Mobile);
        if (mobile is null)
            ModelState.AddModelError(nameof(model.Mobile), "شماره موبایل معتبر نیست. مثال: 09123456789");
        if (model.SlotId < 1)
            ModelState.AddModelError(nameof(model.SlotId), "انتخاب اسلات الزامی است.");

        if (!ModelState.IsValid)
            return View(await BuildFormAsync(model));

        // رزرو اتمیک: فقط وقتی ظرفیت باقی مانده باشد BookedCount افزایش می‌یابد.
        // شرط در UPDATE اجرا می‌شود؛ رقابت همزمان منجر به 0 ردیف و پیام خطا می‌شود.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var reserved = await _context.BookingSlots
            .Where(s => s.Id == model.SlotId && s.IsActive && s.BookedCount < s.Capacity)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.BookedCount, s => s.BookedCount + 1));

        if (reserved != 1)
        {
            await transaction.RollbackAsync();
            model.ErrorMessage = "متأسفانه این اسلات پر شده است. لطفاً اسلات دیگری انتخاب کنید.";
            return View(await BuildFormAsync(model));
        }

        var slot = await _context.BookingSlots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == model.SlotId);

        _context.InquiryLeads.Add(new InquiryLead
        {
            LeadType = "booking",
            FullName = model.FullName.Trim(),
            Mobile = mobile,
            BookingSlotId = model.SlotId,
            Message = model.Message?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Booking reserved. SlotId={SlotId}", model.SlotId);
        model.SuccessMessage = slot is null
            ? "رزرو شما ثبت شد."
            : $"رزرو شما برای {slot.Date:yyyy/MM/dd} ساعت {slot.StartTime} ثبت شد.";
        return View(await BuildFormAsync(model));
    }

    private async Task<BookingFormViewModel> BuildFormAsync(BookingFormViewModel model)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        model.AvailableSlots = await _context.BookingSlots.AsNoTracking()
            .Where(s => s.IsActive && s.Date >= today)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync();
        return model;
    }
}
