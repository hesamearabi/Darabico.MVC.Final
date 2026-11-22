using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>سطح کیفیت متریال (اقتصادی، استاندارد، پریمیوم) با ضریب قیمت.</summary>
public class EstimatorMaterialTier
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public decimal Multiplier { get; set; } = 1.0m;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int EstimatorSettingsId { get; set; }
}
