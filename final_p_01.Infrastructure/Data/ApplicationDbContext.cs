// final_p_01.Infrastructure/Data/ApplicationDbContext.cs
using final_p_01.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<final_p_01.Domain.Entities.AdminUser> AdminUsers { get; set; }

        // 👇 این همون کمد جدیدیه که برای سوالات متداول ساختیم
        public DbSet<Faq> Faqs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // تنظیمات Fluent API برای بهینه‌سازی دیتابیس و سئو
            builder.Entity<Post>()
                .HasIndex(p => p.Slug)
                .IsUnique(); // ایجاد ایندکس منحصربه‌فرد روی Slug برای افزایش فوق‌العاده سرعت جستجوی مقالات

            builder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();
        }
    }
}