using final_p_01.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Infrastructure.Data
{
    // 🚀 تغییر به DbContext سبک (حذف جداول اضافی Identity)
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Faq> Faqs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ایندکس‌های سئو
            builder.Entity<Post>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            // 🚀 Database Fix 1: جلوگیری قطعی از حذف مقالات هنگام حذف دسته‌بندی (Restrict)
            builder.Entity<Post>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🚀 Database Fix 2: حذف خودکار سوالات متداول در صورت پاک شدن مقاله اصلی (Cascade)
            builder.Entity<Faq>()
                .HasOne(f => f.Post)
                .WithMany(p => p.Faqs)
                .HasForeignKey(f => f.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}