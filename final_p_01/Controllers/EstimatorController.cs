using System.ComponentModel.DataAnnotations;
using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

public class EstimatorFormViewModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است."), MaxLength(150)]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است."), MaxLength(20)]
    [Display(Name = "شماره موبایل")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب نوع زمین الزامی است.")]
    [Display(Name = "نوع زمین")]
    public byte CourtType { get; set; }

    [Range(1, 50, ErrorMessage = "تعداد زمین باید بین ۱ تا ۵۰ باشد.")]
    [Display(Name = "تعداد زمین")]
    public int CourtCount { get; set; } = 1;

    [MaxLength(100)]
    [Display(Name = "شهر")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "سطح متریال")]
    public string? MaterialTier { get; set; }

    /// <summary>تله ضداسپم: باید خالی بماند</summary>
    [StringLength(0)]
    public string? Website { get; set; }

    public List<EstimatorMaterialTier> MaterialTiers { get; set; } = new();
    public List<EstimatorCityMultiplier> CityMultipliers { get; set; } = new();
    public string? Disclaimer { get; set; }
    public EstimatorResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

[Route("estimator")]
public class EstimatorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EstimatorController> _logger;

    public EstimatorController(ApplicationDbContext context, ILogger<EstimatorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await BuildFormAsync(new EstimatorFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("FormRateLimit")]
    public async Task<IActionResult> Index([Bind(nameof(EstimatorFormViewModel.FullName), nameof(EstimatorFormViewModel.Mobile), nameof(EstimatorFormViewModel.CourtType), nameof(EstimatorFormViewModel.CourtCount), nameof(EstimatorFormViewModel.City), nameof(EstimatorFormViewModel.MaterialTier), nameof(EstimatorFormViewModel.Website))] EstimatorFormViewModel model)
    {
        // ضداسپم: تله honeypot پر شده است
        if (!string.IsNullOrEmpty(model.Website))
            return base.NotFound();

        var form = await BuildFormAsync(model);

        var mobile = PhoneNormalizer.NormalizeTo09(model.Mobile);
        if (mobile is null)
            ModelState.AddModelError(nameof(model.Mobile), "شماره موبایل معتبر نیست. مثال: 09123456789");

        if (!ModelState.IsValid)
            return FormResponse(form);

        var settings = await _context.EstimatorSettings.AsNoTracking()
            .Include(s => s.MaterialTiers)
            .Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);

        if (settings is null || settings.BasePricePerCourtOutdoor <= 0)
        {
            form.ErrorMessage = "برآوردگر هنوز تنظیم نشده است. لطفاً بعداً مراجعه کنید.";
            return FormResponse(form);
        }

        var result = CostEstimator.Calculate(
            new EstimatorInput(model.CourtType, model.CourtCount, model.City, model.MaterialTier),
            settings.BasePricePerCourtOutdoor,
            settings.BasePricePerCourtIndoor,
            settings.UpperBoundMultiplier,
            settings.CityMultipliers.Where(c => c.IsActive).ToDictionary(c => c.City, c => c.Multiplier),
            settings.MaterialTiers.Where(t => t.IsActive).ToDictionary(t => t.Title, t => t.Multiplier),
            settings.Disclaimer);

        if (result is null)
        {
            form.ErrorMessage = "برای این ترکیب، برآورد امکان‌پذیر نیست. لطفاً با ما تماس بگیرید.";
            return FormResponse(form);
        }

        _context.InquiryLeads.Add(new InquiryLead
        {
            LeadType = "estimator",
            FullName = model.FullName.Trim(),
            Mobile = mobile,
            City = model.City?.Trim(),
            CourtCount = model.CourtCount,
            CourtType = model.CourtType,
            MaterialTier = model.MaterialTier,
            EstimatedMinCost = result.MinCost,
            EstimatedMaxCost = result.MaxCost,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("Estimator lead saved. City={City} Courts={Courts}", model.City, model.CourtCount);

        form.Result = result;
        return FormResponse(form);
    }

    private IActionResult FormResponse(EstimatorFormViewModel form)
    {
        if (!Request.GetTypedHeaders().Accept?.Any(h => h.MediaType.Value == "application/json") ?? true)
            return View("Index", form);

        if (!ModelState.IsValid || form.ErrorMessage is not null)
            return new JsonResult(new
            {
                success = false,
                message = form.ErrorMessage ?? "لطفاً اطلاعات مشخص‌شده را اصلاح کنید.",
                errors = ModelState.Where(p => p.Value?.Errors.Count > 0)
                    .ToDictionary(p => p.Key, p => p.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
            }) { StatusCode = StatusCodes.Status422UnprocessableEntity };

        return Json(new { success = true, result = form.Result });
    }

    private async Task<EstimatorFormViewModel> BuildFormAsync(EstimatorFormViewModel model)
    {
        var settings = await _context.EstimatorSettings.AsNoTracking()
            .Include(s => s.MaterialTiers)
            .Include(s => s.CityMultipliers)
            .FirstOrDefaultAsync(s => s.Id == EstimatorSettings.SingletonId);

        model.MaterialTiers = settings?.MaterialTiers.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList() ?? new();
        model.CityMultipliers = settings?.CityMultipliers.Where(c => c.IsActive).OrderBy(c => c.City).ToList() ?? new();
        model.Disclaimer = settings?.Disclaimer;
        return model;
    }
}
