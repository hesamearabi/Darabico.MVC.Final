using Darabico.Domain.Entities;
using final_p_01.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<Post> Posts { get; }
    DbSet<Category> Categories { get; }
    DbSet<PostSlugHistory> PostSlugHistories { get; }
    DbSet<Faq> Faqs { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectImage> ProjectImages { get; }
    DbSet<InquiryLead> InquiryLeads { get; }
    DbSet<InquiryLeadAttachment> InquiryLeadAttachments { get; }
    DbSet<InquiryLeadNote> InquiryLeadNotes { get; }
    DbSet<SiteSettings> SiteSettings { get; }
    DbSet<EstimatorSettings> EstimatorSettings { get; }
    DbSet<PillarPage> PillarPages { get; }
    DbSet<ProcessStage> ProcessStages { get; }
    DbSet<DocumentItem> DocumentItems { get; }
    DbSet<BookingSlot> BookingSlots { get; }
    DbSet<CityPage> CityPages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
