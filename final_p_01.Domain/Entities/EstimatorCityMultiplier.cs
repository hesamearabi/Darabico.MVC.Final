using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>ضریب هزینه اجرا بر اساس شهر (حمل، نیروی انسانی و...).</summary>
public class EstimatorCityMultiplier
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public decimal Multiplier { get; set; } = 1.0m;

    public bool IsActive { get; set; } = true;

    public int EstimatorSettingsId { get; set; }
}
