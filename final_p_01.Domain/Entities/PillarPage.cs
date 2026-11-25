using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>صفحات پیلار سئو که در /guides/{slug} رندر می‌شوند.</summary>
public class PillarPage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>محتوای اصلی صفحه (HTML) با h2/h3 برای فهرست مطالب خودکار</summary>
    [Required]
    public string ContentHtml { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? MetaTitle { get; set; }

    [MaxLength(320)]
    public string? MetaDescription { get; set; }

    [MaxLength(500)]
    public string? Canonical { get; set; }

    [MaxLength(500)]
    public string? OgImageUrl { get; set; }

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

    public bool IsPublished { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
