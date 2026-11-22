using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

[Route("city")]
public class CityPagesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CityPagesController(ApplicationDbContext context) => _context = context;

    [HttpGet("{slug}"), OutputCache(Duration = 1800)]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return base.NotFound();

        var page = await _context.CityPages.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsPublished);

        // قاعده سخت انتشار: بدون حداقل یک پروژه منتشرشده، صفحه 404 است.
        if (page is null)
            return base.NotFound();

        var publishedProjects = await _context.Projects.AsNoTracking()
            .Where(p => p.IsPublished && p.City == page.CityName)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        if (publishedProjects.Count < CityPage.MinPublishedProjectsToPublish)
            return base.NotFound();

        ViewData["Projects"] = publishedProjects;
        return View(page);
    }
}
