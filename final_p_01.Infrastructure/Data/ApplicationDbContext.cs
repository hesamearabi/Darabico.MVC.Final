using Darabico.Domain.Entities;
using final_p_01.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace final_p_01.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), final_p_01.Application.Common.Interfaces.IAppDbContext
{
    // جداول قبلی سایت دارابیکو
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PostSlugHistory> PostSlugHistories => Set<PostSlugHistory>();
    public DbSet<Faq> Faqs => Set<Faq>();

    // جداول جدید ماژول پروژه‌ها
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();

    // جداول فاز ۲ و ۳
    public DbSet<InquiryLead> InquiryLeads => Set<InquiryLead>();
    public DbSet<InquiryLeadAttachment> InquiryLeadAttachments => Set<InquiryLeadAttachment>();
    public DbSet<InquiryLeadNote> InquiryLeadNotes => Set<InquiryLeadNote>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<EstimatorSettings> EstimatorSettings => Set<EstimatorSettings>();
    public DbSet<PillarPage> PillarPages => Set<PillarPage>();
    public DbSet<ProcessStage> ProcessStages => Set<ProcessStage>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();
    public DbSet<CityPage> CityPages => Set<CityPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectImageConfiguration());
        modelBuilder.ApplyConfiguration(new InquiryLeadConfiguration());
        modelBuilder.ApplyConfiguration(new SiteSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new EstimatorSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new PillarPageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessStageConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentItemConfiguration());
        modelBuilder.ApplyConfiguration(new BookingSlotConfiguration());
        modelBuilder.ApplyConfiguration(new CityPageConfiguration());
        modelBuilder.ApplyConfiguration(new FaqConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());

        modelBuilder.Entity<EstimatorMaterialTier>().Property(t => t.Multiplier).HasPrecision(5, 3);
        modelBuilder.Entity<EstimatorCityMultiplier>().Property(c => c.Multiplier).HasPrecision(5, 3);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(220);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.CourtType).HasConversion<byte>().IsRequired();
        builder.Property(p => p.Description).IsRequired();
        builder.Property(p => p.FeaturedImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(p => p.ThumbnailUrl).IsRequired().HasMaxLength(500);
        builder.Property(p => p.BeforeImageUrl).HasMaxLength(500);
        builder.Property(p => p.AfterImageUrl).HasMaxLength(500);
        builder.Property(p => p.VideoUrl).HasMaxLength(500);
        builder.Property(p => p.MetaTitle).HasMaxLength(160);
        builder.Property(p => p.MetaDescription).HasMaxLength(320);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.IsPublished, p.DisplayOrder });
        builder.HasIndex(p => new { p.IsPublished, p.City, p.CourtType });

        builder.HasMany(p => p.Images)
               .WithOne(i => i.Project)
               .HasForeignKey(i => i.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProjectImageConfiguration : IEntityTypeConfiguration<ProjectImage>
{
    public void Configure(EntityTypeBuilder<ProjectImage> builder)
    {
        builder.ToTable("ProjectImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).IsRequired().HasMaxLength(250);

        builder.HasIndex(i => new { i.ProjectId, i.SortOrder });
    }
}

public class InquiryLeadConfiguration : IEntityTypeConfiguration<InquiryLead>
{
    public void Configure(EntityTypeBuilder<InquiryLead> builder)
    {
        builder.ToTable("InquiryLeads");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LeadType).IsRequired().HasMaxLength(40);
        builder.Property(l => l.FullName).IsRequired().HasMaxLength(150);
        builder.Property(l => l.Mobile).IsRequired().HasMaxLength(15);
        builder.Property(l => l.City).HasMaxLength(100);
        builder.Property(l => l.MaterialTier).HasMaxLength(100);
        builder.Property(l => l.ServiceType).HasMaxLength(100);
        builder.Property(l => l.VisitType).HasMaxLength(50);
        builder.Property(l => l.AccessToken).HasMaxLength(64);

        builder.HasIndex(l => new { l.LeadType, l.CreatedAtUtc });
        builder.HasIndex(l => l.AccessToken);
        builder.HasIndex(l => l.IsHandled);
        builder.HasIndex(l => new { l.Status, l.CreatedAtUtc, l.Id });
        builder.HasIndex(l => new { l.Source, l.CreatedAtUtc });
        builder.ToTable(t => t.HasCheckConstraint("CK_InquiryLeads_Status", "[Status] IN (0,1,2,3,4)"));
        builder.HasOne(l => l.Project).WithMany().HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(l => l.Attachments).WithOne(a => a.Lead).HasForeignKey(a => a.InquiryLeadId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Notes).WithOne(n => n.Lead).HasForeignKey(n => n.InquiryLeadId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.BookingSlot)
               .WithMany()
               .HasForeignKey(l => l.BookingSlotId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CanonicalHost).IsRequired().HasMaxLength(200);
        builder.Property(s => s.WorkshopVideoUrl).HasMaxLength(500);
        builder.Property(s => s.WorkshopVideoPosterUrl).HasMaxLength(500);
        builder.Property(s => s.WorkshopVideoTitle).HasMaxLength(200);
        builder.Property(s => s.WorkshopVideoDescription).HasMaxLength(1000);
        builder.Property(s => s.FeasibilityPdfPath).HasMaxLength(500);
        builder.Property(s => s.FeasibilityPageTitle).IsRequired().HasMaxLength(200);
        builder.Property(s => s.FeasibilityMetaTitle).HasMaxLength(160);
        builder.Property(s => s.FeasibilityMetaDescription).HasMaxLength(320);

        // ردیف یگانه تنظیمات
        builder.HasData(new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            CanonicalHost = "darabico.com", UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

public class EstimatorSettingsConfiguration : IEntityTypeConfiguration<EstimatorSettings>
{
    public void Configure(EntityTypeBuilder<EstimatorSettings> builder)
    {
        builder.ToTable("EstimatorSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Disclaimer).HasMaxLength(1000);
        builder.Property(s => s.UpperBoundMultiplier).HasPrecision(5, 3);

        builder.HasData(new EstimatorSettings
        {
            Id = EstimatorSettings.SingletonId,
            BasePricePerCourtOutdoor = 0,
            BasePricePerCourtIndoor = 0,
            UpperBoundMultiplier = 1.35m, UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        builder.HasMany(s => s.MaterialTiers)
               .WithOne()
               .HasForeignKey(t => t.EstimatorSettingsId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.CityMultipliers)
               .WithOne()
               .HasForeignKey(c => c.EstimatorSettingsId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PillarPageConfiguration : IEntityTypeConfiguration<PillarPage>
{
    public void Configure(EntityTypeBuilder<PillarPage> builder)
    {
        builder.ToTable("PillarPages");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(220);
        builder.Property(p => p.ContentHtml).IsRequired();
        builder.Property(p => p.MetaTitle).HasMaxLength(160);
        builder.Property(p => p.MetaDescription).HasMaxLength(320);
        builder.Property(p => p.Canonical).HasMaxLength(500);
        builder.Property(p => p.OgImageUrl).HasMaxLength(500);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.IsPublished, p.SortOrder });
    }
}

public class ProcessStageConfiguration : IEntityTypeConfiguration<ProcessStage>
{
    public void Configure(EntityTypeBuilder<ProcessStage> builder)
    {
        builder.ToTable("ProcessStages");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired();
        builder.Property(s => s.EstimatedDuration).HasMaxLength(100);
        builder.Property(s => s.IconClass).HasMaxLength(100);

        builder.HasIndex(s => new { s.IsActive, s.SortOrder });
    }
}

public class DocumentItemConfiguration : IEntityTypeConfiguration<DocumentItem>
{
    public void Configure(EntityTypeBuilder<DocumentItem> builder)
    {
        builder.ToTable("DocumentItems");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.Property(d => d.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(d => d.Category).HasMaxLength(100);

        builder.HasIndex(d => new { d.IsPublished, d.SortOrder });
    }
}

public class BookingSlotConfiguration : IEntityTypeConfiguration<BookingSlot>
{
    public void Configure(EntityTypeBuilder<BookingSlot> builder)
    {
        builder.ToTable("BookingSlots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.StartTime).IsRequired().HasMaxLength(5);
        builder.Property(s => s.EndTime).IsRequired().HasMaxLength(5);

        builder.HasIndex(s => new { s.Date, s.IsActive });
    }
}

public class CityPageConfiguration : IEntityTypeConfiguration<CityPage>
{
    public void Configure(EntityTypeBuilder<CityPage> builder)
    {
        builder.ToTable("CityPages");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CityName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(220);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContentHtml).IsRequired();
        builder.Property(c => c.MetaTitle).HasMaxLength(160);
        builder.Property(c => c.MetaDescription).HasMaxLength(320);
        builder.Property(c => c.Canonical).HasMaxLength(500);
        builder.Property(c => c.OgImageUrl).HasMaxLength(500);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("Faqs");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Question).IsRequired().HasMaxLength(500);
        builder.Property(f => f.Answer).IsRequired();
        builder.Property(f => f.PageKey).HasMaxLength(100);

        builder.HasIndex(f => f.PageKey);

        builder.HasOne(f => f.Post)
               .WithMany(p => p.Faqs)
               .HasForeignKey(f => f.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.Property(p => p.MetaTitle).HasMaxLength(160);
        builder.Property(p => p.Canonical).HasMaxLength(500);
        builder.Property(p => p.AuthorName).HasMaxLength(150);

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
