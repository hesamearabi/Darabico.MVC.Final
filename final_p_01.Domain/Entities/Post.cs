using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities
{
    public class Post
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required, MaxLength(200)]
        public string Slug { get; set; }

        [Required]
        public string Content { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        [MaxLength(500)]
        public string? CoverImagePath { get; set; }

        // 🚀 SEO: متن جایگزین برای تصویر شاخص مقاله
        [MaxLength(200)]
        public string? CoverImageAlt { get; set; }

        // 🚀 فیچرهای جدید: فایل صوتی (پادکست) و فایل ضمیمه (PDF/ZIP و...)
        [MaxLength(500)]
        public string? AudioFilePath { get; set; }

        [MaxLength(500)]
        public string? AttachmentFilePath { get; set; }

        // 🚀 ویژگی‌های جدید برای UI/UX حرفه‌ای دانلود فایل
        [MaxLength(150)]
        public string? AttachmentTitle { get; set; }

        [MaxLength(500)]
        public string? AttachmentDescription { get; set; }

        // 🚀 SEO & UX: فیلدهای امتیازدهی ستاره‌ای (برای اسکیمای گوگل بدون نیاز به لاگین)
        public double AverageRating { get; set; } = 5.0; // پیش‌فرض 5 ستاره
        public int TotalVotes { get; set; } = 1; // یک رای پیش‌فرض برای اینکه گوگل اسکیما را قبول کند

        public ICollection<Faq> Faqs { get; set; } = new List<Faq>();
    }
}