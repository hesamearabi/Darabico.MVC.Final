using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using final_p_01.Application.Services;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

public class FeasibilityLeadViewModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است."), MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است."), MaxLength(20)]
    public string Mobile { get; set; } = string.Empty;

    /// <summary>تله ضداسپم</summary>
    [StringLength(0)]
    public string? Website { get; set; }

    public string? PageTitle { get; set; }
    public string? PageIntro { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? DownloadToken { get; set; }
    public bool PdfConfigured { get; set; }
}

[Route("feasibility-study")]
public class FeasibilityController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FeasibilityController> _logger;

    public FeasibilityController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<FeasibilityController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await GetSettingsAsync();
        return View(new FeasibilityLeadViewModel
        {
            PageTitle = settings?.FeasibilityPageTitle,
            PageIntro = settings?.FeasibilityPageIntro,
            MetaTitle = settings?.FeasibilityMetaTitle,
            MetaDescription = settings?.FeasibilityMetaDescription,
            PdfConfigured = IsPdfAvailable(settings)
        });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("FormRateLimit")]
    public async Task<IActionResult> Index([Bind(nameof(FeasibilityLeadViewModel.FullName), nameof(FeasibilityLeadViewModel.Mobile), nameof(FeasibilityLeadViewModel.Website))] FeasibilityLeadViewModel model)
    {
        if (!string.IsNullOrEmpty(model.Website))
            return base.NotFound();

        var settings = await GetSettingsAsync();
        model.PageTitle = settings?.FeasibilityPageTitle;
        model.PageIntro = settings?.FeasibilityPageIntro;
        model.PdfConfigured = IsPdfAvailable(settings);

        var mobile = PhoneNormalizer.NormalizeTo09(model.Mobile);
        if (mobile is null)
            ModelState.AddModelError(nameof(model.Mobile), "شماره موبایل معتبر نیست. مثال: 09123456789");

        if (!ModelState.IsValid)
            return View(model);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        _context.InquiryLeads.Add(new InquiryLead
        {
            LeadType = "feasibility_pdf",
            FullName = model.FullName.Trim(),
            Mobile = mobile,
            AccessToken = token,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("Feasibility study lead saved.");

        model.DownloadToken = token;
        return View(model);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64 || !token.All(Uri.IsHexDigit))
            return base.NotFound();

        var lead = await _context.InquiryLeads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeadType == "feasibility_pdf" && l.AccessToken == token);
        if (lead is null)
            return base.NotFound();

        var settings = await GetSettingsAsync();
        var relativePath = settings?.FeasibilityPdfPath;
        if (string.IsNullOrWhiteSpace(relativePath))
            return base.NotFound();

        var filePath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(filePath))
            return base.NotFound();

        return PhysicalFile(filePath, "application/pdf", "darabico-feasibility-study-sample.pdf");
    }

    private async Task<SiteSettings?> GetSettingsAsync() =>
        await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);

    private bool IsPdfAvailable(SiteSettings? settings)
    {
        var path = settings?.FeasibilityPdfPath;
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var filePath = Path.Combine(_environment.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return System.IO.File.Exists(filePath);
    }
}
