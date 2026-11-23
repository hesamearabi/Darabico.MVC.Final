using Darabico.Domain.Enums;
using final_p_01.Infrastructure;
using Darabico.Web.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darabico.Web.Controllers;

public class ProjectsController(ApplicationDbContext dbContext) : Controller
{
    private const int DefaultPageSize = 6;

    [HttpGet("/projects")]
    public async Task<IActionResult> Index(
        [FromQuery] string? city,
        [FromQuery] CourtType? courtType,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        var baseQuery = dbContext.Projects
            .AsNoTracking()
            .Where(p => p.IsPublished);

        // دریافت لیست شهرهایی که حداقل یک پروژه منتشرشده دارند
        var cities = await baseQuery
            .Select(p => p.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        var filteredQuery = ApplyFilters(baseQuery, city, courtType);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        page = Math.Min(page, Math.Max(1, (int)Math.Ceiling(totalCount / (double)DefaultPageSize)));

        var items = await filteredQuery
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(p => new ProjectCardViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                City = p.City,
                CourtCount = p.CourtCount,
                CourtType = p.CourtType,
                ThumbnailUrl = p.ThumbnailUrl,
                ExecutionYear = p.ExecutionYear
            })
            .ToListAsync(cancellationToken);

        var model = new ProjectIndexViewModel
        {
            Projects = items,
            AvailableCities = cities,
            SelectedCity = city,
            SelectedCourtType = courtType,
            CurrentPage = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount
        };

        return View(model);
    }

    [HttpGet("/projects/load-more")]
    public async Task<IActionResult> LoadMore(
        [FromQuery] string? city,
        [FromQuery] CourtType? courtType,
        [FromQuery] int page = 2,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        var baseQuery = dbContext.Projects
            .AsNoTracking()
            .Where(p => p.IsPublished);

        var filteredQuery = ApplyFilters(baseQuery, city, courtType);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(p => new ProjectCardViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                City = p.City,
                CourtCount = p.CourtCount,
                CourtType = p.CourtType,
                ThumbnailUrl = p.ThumbnailUrl,
                ExecutionYear = p.ExecutionYear
            })
            .ToListAsync(cancellationToken);

        bool hasMore = page * DefaultPageSize < totalCount;
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";

        return PartialView("_ProjectCardsPartial", items);
    }

    [HttpGet("/projects/details/{slug}")]
    public IActionResult LegacyDetails(string slug)
    {
        return RedirectPermanent($"/projects/{Uri.EscapeDataString(slug)}");
    }

    [HttpGet("/projects/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var project = await dbContext.Projects
            .AsNoTracking()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug, cancellationToken);

        if (project is null)
            return NotFound();

        var canonicalUrl = $"{Request.Scheme}://{Request.Host}/projects/{Uri.EscapeDataString(project.Slug)}";

        var model = new ProjectDetailViewModel
        {
            Id = project.Id,
            Title = project.Title,
            Slug = project.Slug,
            City = project.City,
            CourtCount = project.CourtCount,
            CourtType = project.CourtType,
            ExecutionYear = project.ExecutionYear,
            ExecutionDurationMonths = project.ExecutionDurationMonths,
            Description = project.Description,
            FeaturedImageUrl = project.FeaturedImageUrl,
            BeforeImageUrl = project.BeforeImageUrl,
            AfterImageUrl = project.AfterImageUrl,
            VideoUrl = project.VideoUrl,
            MetaTitle = !string.IsNullOrWhiteSpace(project.MetaTitle)
                ? project.MetaTitle
                : $"{project.Title} | پروژه‌های ساخت زمین پدل دارابیکو",
            MetaDescription = !string.IsNullOrWhiteSpace(project.MetaDescription)
                ? project.MetaDescription
                : $"جزئیات اجرای پروژه {project.Title} در شهر {project.City} شامل {project.CourtCount} زمین پدل توسط تیم مهندسی دارابیکو.",
            CanonicalUrl = canonicalUrl,
            GalleryImages = project.Images.Select(i => new ProjectGalleryItemViewModel
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                Width = i.Width > 0 ? i.Width : 900,
                Height = i.Height > 0 ? i.Height : 600
            }).ToList()
        };

        return View(model);
    }

    private static IQueryable<Domain.Entities.Project> ApplyFilters(
        IQueryable<Domain.Entities.Project> query,
        string? city,
        CourtType? courtType)
    {
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City == city.Trim());
        }

        if (courtType.HasValue && Enum.IsDefined(courtType.Value))
        {
            query = query.Where(p => p.CourtType == courtType.Value);
        }

        return query;
    }
}