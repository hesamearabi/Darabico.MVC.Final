using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.ViewComponents;

/// <summary>منوی داینامیک: زیرمجموعه‌های راهنمای جامع 0 تا 100 + آخرین مقالات مجله.</summary>
public class SiteNavViewComponent : ViewComponent
{
    private const string GuideSlug = "padel-court-construction-guide";
    private const int LatestPostsCount = 5;

    private readonly ApplicationDbContext _context;

    public SiteNavViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var guideChildren = await _context.Posts
            .AsNoTracking()
            .Where(p => p.IsPublished && p.ParentPost != null && p.ParentPost.Slug == GuideSlug)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PostCardViewModel
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                CoverImagePath = p.CoverImagePath,
                CoverImageAlt = p.CoverImageAlt,
                MetaDescription = p.MetaDescription,
                CreatedAt = p.CreatedAt,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .ToListAsync();

        var latestPosts = await _context.Posts
            .AsNoTracking()
            .Where(p => p.IsPublished && p.Slug != GuideSlug)
            .OrderByDescending(p => p.CreatedAt)
            .Take(LatestPostsCount)
            .Select(p => new PostCardViewModel
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                CoverImagePath = p.CoverImagePath,
                CoverImageAlt = p.CoverImageAlt,
                MetaDescription = p.MetaDescription,
                CreatedAt = p.CreatedAt,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .ToListAsync();

        return View(new SiteNavViewModel(guideChildren, latestPosts));
    }
}

public record SiteNavViewModel(List<PostCardViewModel> GuideChildren, List<PostCardViewModel> LatestPosts);
