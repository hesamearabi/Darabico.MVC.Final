using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.ViewComponents;

public class WorkshopVideoViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public WorkshopVideoViewComponent(ApplicationDbContext context) => _context = context;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = await _context.SiteSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId);

        return View("~/Views/Shared/Partials/_WorkshopVideo.cshtml", settings);
    }
}
