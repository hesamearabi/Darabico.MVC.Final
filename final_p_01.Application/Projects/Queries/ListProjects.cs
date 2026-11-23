using Darabico.Domain.Entities;
using Darabico.Domain.Enums;
using final_p_01.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Application.Projects.Queries;

public sealed record ListProjectsQuery(string? City, CourtType? CourtType, int Page = 1, bool ClampToLastPage = true);
public sealed record ProjectPage(List<Project> Projects, List<string> Cities, int Page, int PageSize, int TotalCount)
{
    public bool HasMore => (long)Page * PageSize < TotalCount;
}

public sealed class ListProjectsHandler(IAppDbContext db)
{
    public async Task<ProjectPage> Handle(ListProjectsQuery request, CancellationToken cancellationToken = default)
    {
        const int pageSize = 6;
        var page = Math.Clamp(request.Page, 1, int.MaxValue / pageSize);
        var published = db.Projects.AsNoTracking().Where(p => p.IsPublished);
        var filtered = published;
        if (!string.IsNullOrWhiteSpace(request.City))
            filtered = filtered.Where(p => p.City == request.City.Trim());
        if (request.CourtType.HasValue && Enum.IsDefined(request.CourtType.Value))
            filtered = filtered.Where(p => p.CourtType == request.CourtType.Value);
        var total = await filtered.CountAsync(cancellationToken);
        if (request.ClampToLastPage)
            page = Math.Min(page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        var projects = await filtered.OrderBy(p => p.DisplayOrder).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var cities = await published.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync(cancellationToken);
        return new(projects, cities, page, pageSize, total);
    }
}
