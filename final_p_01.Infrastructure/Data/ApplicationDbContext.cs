using Darabico.Domain.Entities;
using final_p_01.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace final_p_01.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // جداول قبلی سایت دارابی‌کو
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PostSlugHistory> PostSlugHistories => Set<PostSlugHistory>();
    public DbSet<Faq> Faqs => Set<Faq>();

    // جداول جدید ماژول پروژه‌ها
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectImageConfiguration());
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