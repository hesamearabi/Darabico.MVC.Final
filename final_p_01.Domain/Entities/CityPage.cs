using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>
/// صفحه لندینگ شهر؛ طبق قاعده سخت انتشار، فقط با حداقل یک پروژه منتشرشده
/// در همان شهر قابل انتشار است.
/// </summary>
public class CityPage
{
    public const int MinPublishedProjectsToPublish = 1;

    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string CityName { get; set; } = string.Empty;

    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

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

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
