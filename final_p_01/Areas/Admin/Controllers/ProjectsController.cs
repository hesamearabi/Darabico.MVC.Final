using Darabico.Domain.Entities;
using final_p_01.Infrastructure;
using Darabico.Web.Areas.Admin.ViewModels;
using Darabico.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darabico.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/projects")]
public class ProjectsController(
    ApplicationDbContext dbContext,
    IImageService imageService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id)
            .Select(p => new AdminProjectListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                City = p.City,
                CourtCount = p.CourtCount,
                CourtType = p.CourtType,
                ExecutionYear = p.ExecutionYear,
                ThumbnailUrl = p.ThumbnailUrl,
                DisplayOrder = p.DisplayOrder,
                IsPublished = p.IsPublished,
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return View(projects);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        var model = new ProjectFormViewModel
        {
            ExecutionYear = PersianDateHelper.GetCurrentPersianYear(),
            IsPublished = true
        };
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.FeaturedImageFile is null || model.FeaturedImageFile.Length == 0)
            ModelState.AddModelError(nameof(model.FeaturedImageFile), "آپلود تصویر شاخص الزامی است.");

        ValidateNewGalleryItems(model);

        if (!ModelState.IsValid)
            return View(model);

        var slug = await EnsureUniqueSlugAsync(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug,
            excludeProjectId: null,
            cancellationToken);

        try
        {
            var featuredResult = await imageService.SaveProjectImageAsync(
                model.FeaturedImageFile!,
                subFolder: "projects/featured",
                generateThumbnail: true,
                maxWidth: 1200,
                cancellationToken: cancellationToken);

            string? beforeUrl = null;
            if (model.BeforeImageFile is { Length: > 0 })
            {
                var beforeRes = await imageService.SaveProjectImageAsync(
                    model.BeforeImageFile, "projects/compare", false, 1200, cancellationToken);
                beforeUrl = beforeRes.ImageUrl;
            }

            string? afterUrl = null;
            if (model.AfterImageFile is { Length: > 0 })
            {
                var afterRes = await imageService.SaveProjectImageAsync(
                    model.AfterImageFile, "projects/compare", false, 1200, cancellationToken);
                afterUrl = afterRes.ImageUrl;
            }

            string? videoUrl = model.VideoUrl?.Trim();
            if (model.VideoFile is { Length: > 0 })
            {
                videoUrl = await imageService.SaveVideoAsync(model.VideoFile, "projects/videos", cancellationToken);
            }

            var project = new Project
            {
                Title = model.Title.Trim(),
                Slug = slug,
                City = model.City.Trim(),
                CourtCount = model.CourtCount,
                CourtType = model.CourtType,
                ExecutionYear = model.ExecutionYear,
                ExecutionDurationMonths = model.ExecutionDurationMonths,
                Description = model.Description,
                FeaturedImageUrl = featuredResult.ImageUrl,
                ThumbnailUrl = featuredResult.ThumbnailUrl!,
                BeforeImageUrl = beforeUrl,
                AfterImageUrl = afterUrl,
                VideoUrl = videoUrl,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished,
                MetaTitle = model.MetaTitle?.Trim(),
                MetaDescription = model.MetaDescription?.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await AppendNewGalleryImagesAsync(project, model, cancellationToken);

            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] = "پروژه جدید با موفقیت ثبت شد.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return NotFound();

        var model = new ProjectFormViewModel
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
            ExistingFeaturedImageUrl = project.FeaturedImageUrl,
            ExistingThumbnailUrl = project.ThumbnailUrl,
            ExistingBeforeImageUrl = project.BeforeImageUrl,
            ExistingAfterImageUrl = project.AfterImageUrl,
            VideoUrl = project.VideoUrl,
            DisplayOrder = project.DisplayOrder,
            IsPublished = project.IsPublished,
            MetaTitle = project.MetaTitle,
            MetaDescription = project.MetaDescription,
            ExistingGalleryImages = project.Images.Select(i => new ExistingGalleryImageViewModel
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                SortOrder = i.SortOrder
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectFormViewModel model, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return NotFound();

        ValidateNewGalleryItems(model);

        if (!ModelState.IsValid)
        {
            model.ExistingFeaturedImageUrl = project.FeaturedImageUrl;
            model.ExistingThumbnailUrl = project.ThumbnailUrl;
            model.ExistingBeforeImageUrl = project.BeforeImageUrl;
            model.ExistingAfterImageUrl = project.AfterImageUrl;
            return View(model);
        }

        try
        {
            project.Slug = await EnsureUniqueSlugAsync(
                string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug,
                excludeProjectId: project.Id,
                cancellationToken);

            // جایگزینی تصویر شاخص در صورت آپلود جدید
            if (model.FeaturedImageFile is { Length: > 0 })
            {
                var oldFeatured = project.FeaturedImageUrl;
                var oldThumb = project.ThumbnailUrl;

                var featuredRes = await imageService.SaveProjectImageAsync(
                    model.FeaturedImageFile, "projects/featured", true, 1200, cancellationToken);

                project.FeaturedImageUrl = featuredRes.ImageUrl;
                project.ThumbnailUrl = featuredRes.ThumbnailUrl!;

                imageService.DeleteFile(oldFeatured);
                imageService.DeleteFile(oldThumb);
            }

            // مدیریت تصویر قبل از اجرا
            if (model.RemoveBeforeImage && !string.IsNullOrEmpty(project.BeforeImageUrl))
            {
                imageService.DeleteFile(project.BeforeImageUrl);
                project.BeforeImageUrl = null;
            }
            if (model.BeforeImageFile is { Length: > 0 })
            {
                imageService.DeleteFile(project.BeforeImageUrl);
                var beforeRes = await imageService.SaveProjectImageAsync(
                    model.BeforeImageFile, "projects/compare", false, 1200, cancellationToken);
                project.BeforeImageUrl = beforeRes.ImageUrl;
            }

            // مدیریت تصویر بعد از اجرا
            if (model.RemoveAfterImage && !string.IsNullOrEmpty(project.AfterImageUrl))
            {
                imageService.DeleteFile(project.AfterImageUrl);
                project.AfterImageUrl = null;
            }
            if (model.AfterImageFile is { Length: > 0 })
            {
                imageService.DeleteFile(project.AfterImageUrl);
                var afterRes = await imageService.SaveProjectImageAsync(
                    model.AfterImageFile, "projects/compare", false, 1200, cancellationToken);
                project.AfterImageUrl = afterRes.ImageUrl;
            }

            // مدیریت ویدیو
            if (model.RemoveVideo)
            {
                imageService.DeleteFile(project.VideoUrl);
                project.VideoUrl = null;
            }
            else if (model.VideoFile is { Length: > 0 })
            {
                imageService.DeleteFile(project.VideoUrl);
                project.VideoUrl = await imageService.SaveVideoAsync(model.VideoFile, "projects/videos", cancellationToken);
            }
            else
            {
                var newVideoUrl = model.VideoUrl?.Trim();
                if (project.VideoUrl != newVideoUrl)
                {
                    imageService.DeleteFile(project.VideoUrl);
                    project.VideoUrl = newVideoUrl;
                }
            }

            project.Title = model.Title.Trim();
            project.City = model.City.Trim();
            project.CourtCount = model.CourtCount;
            project.CourtType = model.CourtType;
            project.ExecutionYear = model.ExecutionYear;
            project.ExecutionDurationMonths = model.ExecutionDurationMonths;
            project.Description = model.Description;
            project.DisplayOrder = model.DisplayOrder;
            project.IsPublished = model.IsPublished;
            project.MetaTitle = model.MetaTitle?.Trim();
            project.MetaDescription = model.MetaDescription?.Trim();
            project.UpdatedAtUtc = DateTime.UtcNow;

            // به‌روزرسانی AltText و SortOrder تصاویر موجود گالری
            foreach (var existingDto in model.ExistingGalleryImages)
            {
                var entityImg = project.Images.FirstOrDefault(i => i.Id == existingDto.Id);
                if (entityImg is not null)
                {
                    entityImg.AltText = existingDto.AltText.Trim();
                    entityImg.SortOrder = existingDto.SortOrder;
                }
            }

            // افزودن تصاویر جدید به گالری
            await AppendNewGalleryImagesAsync(project, model, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "تغییرات پروژه با موفقیت ذخیره شد.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
            return NotFound();

        // حذف تمام فایل‌های فیزیکی مرتبط
        imageService.DeleteFile(project.FeaturedImageUrl);
        imageService.DeleteFile(project.ThumbnailUrl);
        imageService.DeleteFile(project.BeforeImageUrl);
        imageService.DeleteFile(project.AfterImageUrl);
        imageService.DeleteFile(project.VideoUrl);

        foreach (var img in project.Images)
        {
            imageService.DeleteFile(img.ImageUrl);
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "پروژه و تمام فایل‌های رسانه‌ای آن حذف شدند.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete-gallery-image/{imageId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGalleryImage(int imageId, CancellationToken cancellationToken)
    {
        var img = await dbContext.ProjectImages.FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);
        if (img is null)
            return NotFound();

        imageService.DeleteFile(img.ImageUrl);
        dbContext.ProjectImages.Remove(img);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true });
    }

    [HttpPost("toggle-publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
            return NotFound();

        project.IsPublished = !project.IsPublished;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update-order/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder(int id, int displayOrder, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
            return NotFound();

        project.DisplayOrder = displayOrder;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    private void ValidateNewGalleryItems(ProjectFormViewModel model)
    {
        if (model.NewGalleryFiles is null || model.NewGalleryFiles.Count == 0)
            return;

        for (int i = 0; i < model.NewGalleryFiles.Count; i++)
        {
            var file = model.NewGalleryFiles[i];
            if (file is null || file.Length == 0)
                continue;

            var alt = (model.NewGalleryAltTexts is not null && i < model.NewGalleryAltTexts.Count)
                ? model.NewGalleryAltTexts[i]
                : null;

            if (string.IsNullOrWhiteSpace(alt))
            {
                ModelState.AddModelError(string.Empty, $"وارد کردن متن جایگزین (Alt) برای تصویر جدید شماره {i + 1} گالری الزامی است.");
            }
        }
    }

    private async Task AppendNewGalleryImagesAsync(
        Project project,
        ProjectFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.NewGalleryFiles is null || model.NewGalleryFiles.Count == 0)
            return;

        for (int i = 0; i < model.NewGalleryFiles.Count; i++)
        {
            var file = model.NewGalleryFiles[i];
            if (file is null || file.Length == 0)
                continue;

            var alt = model.NewGalleryAltTexts![i].Trim();
            var sortOrder = (model.NewGallerySortOrders is not null && i < model.NewGallerySortOrders.Count)
                ? model.NewGallerySortOrders[i]
                : (project.Images.Count + i + 1);

            var processed = await imageService.SaveProjectImageAsync(
                file, "projects/gallery", generateThumbnail: false, maxWidth: 1200, cancellationToken);

            project.Images.Add(new ProjectImage
            {
                ImageUrl = processed.ImageUrl,
                AltText = alt,
                Width = processed.Width,
                Height = processed.Height,
                SortOrder = sortOrder
            });
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(
        string rawSlug,
        int? excludeProjectId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugHelper.GenerateSlug(rawSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = $"project-{Guid.NewGuid():N}"[..16];

        var candidate = baseSlug;
        int counter = 2;

        while (await dbContext.Projects.AnyAsync(
            p => p.Slug == candidate && (!excludeProjectId.HasValue || p.Id != excludeProjectId.Value),
            cancellationToken))
        {
            candidate = $"{baseSlug}-{counter++}";
        }

        return candidate;
    }
}