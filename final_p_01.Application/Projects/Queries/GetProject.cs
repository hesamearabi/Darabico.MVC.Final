using Darabico.Domain.Entities;
using final_p_01.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Application.Projects.Queries;

public sealed class GetProjectHandler(IAppDbContext db)
{
    public Task<Project?> Handle(string slug, CancellationToken cancellationToken = default) =>
        db.Projects.AsNoTracking().Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Faqs)
            .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug, cancellationToken);
}
