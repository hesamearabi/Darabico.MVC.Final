using System.ComponentModel.DataAnnotations;
using Darabico.Domain.Enums;

namespace Darabico.Domain.Entities;

public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان پروژه الزامی است.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "نامک (Slug) الزامی است.")]
    [MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام شهر الزامی است.")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(1, 500, ErrorMessage = "تعداد زمین باید حداقل ۱ باشد.")]
    public int CourtCount { get; set; }

    public CourtType CourtType { get; set; }

    [Range(1380, 1500, ErrorMessage = "سال اجرا معتبر نیست.")]
    public int ExecutionYear { get; set; }

    [Range(1, 120, ErrorMessage = "مدت زمان اجرا معتبر نیست.")]
    public int ExecutionDurationMonths { get; set; }

    [Required(ErrorMessage = "توضیحات پروژه الزامی است.")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FeaturedImageUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BeforeImageUrl { get; set; }

    [MaxLength(500)]
    public string? AfterImageUrl { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    [MaxLength(160)]
    public string? MetaTitle { get; set; }

    [MaxLength(320)]
    public string? MetaDescription { get; set; }

    public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
}