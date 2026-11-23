using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using final_p_01.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.ViewComponents;

public class InquiryFormViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string source, int? projectRef = null)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1) ?? new SiteSettings();
        return View("~/Views/Shared/Partials/_InquiryForm.cshtml", new InquiryFormViewModel { Source = source, ProjectRef = projectRef, Settings = settings });
    }
}
