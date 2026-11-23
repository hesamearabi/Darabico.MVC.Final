using System.ComponentModel.DataAnnotations;
using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

public class AfterSalesFormViewModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است."), MaxLength(150)]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است."), MaxLength(20)]
    [Display(Name = "شماره موبایل")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب نوع خدمت الزامی است."), MaxLength(100)]
    [Display(Name = "نوع خدمت")]
    public string ServiceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب نوع مراجعه الزامی است."), MaxLength(50)]
    [Display(Name = "نوع مراجعه")]
    public string VisitType { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "توضیحات")]
    public string? Message { get; set; }

    /// <summary>تله ضداسپم</summary>
    [StringLength(0)]
    public string? Website { get; set; }

    public string? Intro { get; set; }
    public string? SuccessMessage { get; set; }

    public static readonly string[] AllowedServiceTypes =
    {
        "تعمیر چمن مصنوعی", "تعویض شیشه", "سرویس نورپردازی", "تعمیر تور و پنل", "سایر خدمات"
    };

    public static readonly string[] AllowedVisitTypes = { "حضوری", "آنلاین" };
}

[Route("after-sales")]
public class AfterSalesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AfterSalesController> _logger;

    public AfterSalesController(ApplicationDbContext context, ILogger<AfterSalesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var form = await BuildFormAsync(new AfterSalesFormViewModel());
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("FormRateLimit")]
    public async Task<IActionResult> Index([Bind(nameof(AfterSalesFormViewModel.FullName), nameof(AfterSalesFormViewModel.Mobile), nameof(AfterSalesFormViewModel.ServiceType), nameof(AfterSalesFormViewModel.VisitType), nameof(AfterSalesFormViewModel.Message), nameof(AfterSalesFormViewModel.Website))] AfterSalesFormViewModel model)
    {
        if (!string.IsNullOrEmpty(model.Website))
            return base.NotFound();

        var form = await BuildFormAsync(model);

        var mobile = PhoneNormalizer.NormalizeTo09(model.Mobile);
        if (mobile is null)
            ModelState.AddModelError(nameof(model.Mobile), "شماره موبایل معتبر نیست. مثال: 09123456789");
        if (!AfterSalesFormViewModel.AllowedServiceTypes.Contains(model.ServiceType))
            ModelState.AddModelError(nameof(model.ServiceType), "نوع خدمت معتبر نیست.");
        if (!AfterSalesFormViewModel.AllowedVisitTypes.Contains(model.VisitType))
            ModelState.AddModelError(nameof(model.VisitType), "نوع مراجعه معتبر نیست.");

        if (!ModelState.IsValid || mobile is null)
            return View(form);

        _context.InquiryLeads.Add(new InquiryLead
        {
            LeadType = "aftersales",
            FullName = model.FullName.Trim(),
            Mobile = mobile,
            ServiceType = model.ServiceType,
            VisitType = model.VisitType,
            Message = model.Message?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("After-sales inquiry saved. Service={Service}", model.ServiceType);

        form.SuccessMessage = "درخواست شما ثبت شد. کارشناسان ما در اسرع وقت با شما تماس می‌گیرند.";
        return View(await BuildFormAsync(form));
    }

    private async Task<AfterSalesFormViewModel> BuildFormAsync(AfterSalesFormViewModel model)
    {
        var settings = await _context.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);
        model.Intro = settings?.AfterSalesIntro;
        return model;
    }
}
