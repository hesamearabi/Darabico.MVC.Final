using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities
{
    public class Post
    {
        public int Id { get; set; }

        [Required, MaxLength(200)] // 🚀 پرفورمنس: محدودیت طول برای جلوگیری از NVARCHAR(MAX)
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

        public ICollection<Faq> Faqs { get; set; } = new List<Faq>();
    }
}