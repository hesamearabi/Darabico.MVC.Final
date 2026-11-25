using Darabico.Domain.Entities;
using Darabico.Domain.Enums;
using Darabico.Web.ViewModels.Public;
using final_p_01.Application.Projects.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Darabico.Web.Controllers;

public class ProjectsController(ListProjectsHandler listProjects, GetProjectHandler getProject) : Controller
{
    [HttpGet("/projects")]
    public async Task<IActionResult> Index(
        [FromQuery] string? city,
        [FromQuery] CourtType? courtType,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await listProjects.Handle(new(city, courtType, page), cancellationToken);
        return View(new ProjectIndexViewModel
        {
            Projects = result.Projects.Select(ToCard).ToList(),
            AvailableCities = result.Cities,
            SelectedCity = city,
            SelectedCourtType = courtType,
            CurrentPage = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        });
    }

    [HttpGet("/projects/load-more")]
    public async Task<IActionResult> LoadMore(
        [FromQuery] string? city,
        [FromQuery] CourtType? courtType,
        [FromQuery] int page = 2,
        CancellationToken cancellationToken = default)
    {
        var result = await listProjects.Handle(new(city, courtType, page, ClampToLastPage: false), cancellationToken);
        Response.Headers["X-Has-More"] = result.HasMore ? "true" : "false";
        return PartialView("_ProjectCardsPartial", result.Projects.Select(ToCard).ToList());
    }

    [HttpGet("/projects/details/{slug}")]
    public IActionResult LegacyDetails(string slug)
    {
        return RedirectPermanent($"/projects/{Uri.EscapeDataString(slug)}");
    }

    [HttpGet("/projects/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var project = await getProject.Handle(slug, cancellationToken);
        if (project is null) return NotFound();

        return View(new ProjectDetailViewModel
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
            FeaturedImageAlt = project.FeaturedImageAlt,
            BeforeImageUrl = project.BeforeImageUrl,
            AfterImageUrl = project.AfterImageUrl,
            VideoUrl = project.VideoUrl,
            AudioFilePath = project.AudioFilePath,
            AttachmentFilePath = project.AttachmentFilePath,
            AttachmentTitle = project.AttachmentTitle,
            AttachmentDescription = project.AttachmentDescription,
            Faqs = project.Faqs.OrderBy(f => f.Id).Select(f => new ProjectFaqItemViewModel
            {
                Question = f.Question,
                Answer = f.Answer
            }).ToList(),
            MetaTitle = !string.IsNullOrWhiteSpace(project.MetaTitle)
                ? project.MetaTitle
                : $"{project.Title} | پروژه‌های ساخت زمین پدل دارابیکو",
            MetaDescription = !string.IsNullOrWhiteSpace(project.MetaDescription)
                ? project.MetaDescription
                : $"جزئیات اجرای پروژه {project.Title} در شهر {project.City} شامل {project.CourtCount} زمین پدل توسط تیم مهندسی دارابیکو.",
            CanonicalUrl = $"{Request.Scheme}://{Request.Host}/projects/{Uri.EscapeDataString(project.Slug)}",
            GalleryImages = project.Images.Select(i => new ProjectGalleryItemViewModel
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                Width = i.Width > 0 ? i.Width : 900,
                Height = i.Height > 0 ? i.Height : 600
            }).ToList()
        });
    }

    private static ProjectCardViewModel ToCard(Project project) => new()
    {
        Id = project.Id,
        Title = project.Title,
        Slug = project.Slug,
        City = project.City,
        CourtCount = project.CourtCount,
        CourtType = project.CourtType,
        ThumbnailUrl = project.ThumbnailUrl,
        ExecutionYear = project.ExecutionYear
    };
}
