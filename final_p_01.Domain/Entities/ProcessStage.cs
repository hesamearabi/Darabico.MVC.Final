using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>مراحل تایم‌لاین عمودی فرایند اجرا در صفحه /process.</summary>
public class ProcessStage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EstimatedDuration { get; set; }

    [MaxLength(100)]
    public string? IconClass { get; set; }

    public int StepNumber { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
