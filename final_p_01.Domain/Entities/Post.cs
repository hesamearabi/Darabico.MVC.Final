using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

public class Post
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Slug { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    [MaxLength(160)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? Canonical { get; set; }

    [MaxLength(150)]
    public string? AuthorName { get; set; }

    /// <summary>تاریخ انتشار رسمی برای نمایش شمسی؛ خالی بودن از CreatedAt استفاده می‌شود.</summary>
    public DateTime? PublishedAtUtc { get; set; }

    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    [MaxLength(200)]
    public string? CoverImageAlt { get; set; }

    [MaxLength(500)]
    public string? AudioFilePath { get; set; }

    [MaxLength(500)]
    public string? AttachmentFilePath { get; set; }

    [MaxLength(150)]
    public string? AttachmentTitle { get; set; }

    [MaxLength(500)]
    public string? AttachmentDescription { get; set; }

    public double AverageRating { get; set; } = 0.0;
    public int TotalVotes { get; set; } = 0;

    public ICollection<Faq> Faqs { get; set; } = new List<Faq>();
    public ICollection<PostSlugHistory> SlugHistories { get; set; } = new List<PostSlugHistory>();

    /// <summary>صفحه پیلار والد (مثلاً راهنمای جامع 0 تا 100)؛ خالی یعنی مقاله مستقل.</summary>
    public int? ParentPostId { get; set; }
    public Post? ParentPost { get; set; }
    public ICollection<Post> Children { get; set; } = new List<Post>();
}