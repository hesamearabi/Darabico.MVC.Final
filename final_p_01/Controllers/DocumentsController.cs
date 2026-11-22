using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Controllers;

[Route("technical-docs")]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet, OutputCache(Duration = 1800)]
    public async Task<IActionResult> Index()
    {
        var documents = await _context.DocumentItems.AsNoTracking()
            .Where(d => d.IsPublished)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();
        return View(documents);
    }

    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.DocumentItems.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.IsPublished);
        if (document is null)
            return base.NotFound();

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(filePath))
            return base.NotFound();

        return PhysicalFile(filePath, "application/octet-stream", Path.GetFileName(filePath));
    }
}
