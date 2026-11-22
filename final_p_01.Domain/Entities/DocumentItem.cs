using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>کاتالوگ و دیتاشیت‌های قابل دانلود در /technical-docs.</summary>
public class DocumentItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
