using final_p_01.Infrastructure.Data;
using final_p_01.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace final_p_01.ViewComponents;

public static class SiteNavigationCache
{
    private static CancellationTokenSource revision = new();
    public static IChangeToken Token => new CancellationChangeToken(Volatile.Read(ref revision).Token);
    public static void Invalidate()
    {
        var previous = Interlocked.Exchange(ref revision, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }
}

public class SiteNavViewModel
{
    public List<PostCardViewModel> GuideChildren { get; set; } = new();
    public List<PostCardViewModel> LatestPosts { get; set; } = new();
    public List<final_p_01.Domain.Entities.Category> Categories { get; set; } = new();
}

public class SiteNavViewComponent(ApplicationDbContext context, IMemoryCache cache) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await cache.GetOrCreateAsync("site-navigation", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            entry.AddExpirationToken(SiteNavigationCache.Token);
            var guideId = await context.Posts.AsNoTracking()
                .Where(p => p.IsPublished && p.Slug == "padel-court-construction-guide")
                .Select(p => (int?)p.Id).FirstOrDefaultAsync();
            var children = guideId.HasValue
                ? await context.Posts.AsNoTracking().Where(p => p.IsPublished && p.ParentPostId == guideId)
                    .OrderBy(p => p.Title).Select(p => new PostCardViewModel { Id = p.Id, Slug = p.Slug, Title = p.Title }).ToListAsync()
                : new List<PostCardViewModel>();
            var latest = await context.Posts.AsNoTracking()
                .Where(p => p.IsPublished && p.Slug != "padel-court-construction-guide")
                .OrderByDescending(p => p.CreatedAt).Take(5)
                .Select(p => new PostCardViewModel { Id = p.Id, Slug = p.Slug, Title = p.Title }).ToListAsync();
            var categories = await context.Categories.AsNoTracking()
                .Where(c => context.Posts.Any(p => p.IsPublished && p.CategoryId == c.Id))
                .OrderBy(c => c.Name).ToListAsync();
            return new SiteNavViewModel { GuideChildren = children, LatestPosts = latest, Categories = categories };
        });
        return View(model);
    }
}
