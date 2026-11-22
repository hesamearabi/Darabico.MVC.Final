using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

[Route("process")]
public class ProcessController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProcessController(ApplicationDbContext context) => _context = context;

    [HttpGet, OutputCache(Duration = 1800)]
    public async Task<IActionResult> Index()
    {
        var stages = await _context.ProcessStages.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var settings = await _context.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);

        ViewData["PaymentTermsHtml"] = settings?.PaymentTermsHtml;
        ViewData["WarrantyHtml"] = settings?.WarrantyHtml;
        return View(stages);
    }
}
