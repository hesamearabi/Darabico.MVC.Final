using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

[Route("guides")]
public class GuidesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GuidesController(ApplicationDbContext context) => _context = context;

    [HttpGet("{slug}"), OutputCache(Duration = 1800)]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return base.NotFound();

        var page = await _context.PillarPages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (page is null)
            return base.NotFound();

        var faqs = await _context.Faqs.AsNoTracking()
            .Where(f => f.PageKey == $"pillar:{slug}")
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        ViewData["Faqs"] = faqs;
        return View(page);
    }
}
