using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

/// <summary>تنظیمات برآوردگر هزینه؛ همیشه فقط یک ردیف با Id = 1 وجود دارد.</summary>
public class EstimatorSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    /// <summary>قیمت پایه به تومان برای هر متر مربع زمین استاندارد (ساله/اپن)</summary>
    public long BasePricePerCourtOutdoor { get; set; }

    /// <summary>قیمت پایه برای زمین سرپوشیده</summary>
    public long BasePricePerCourtIndoor { get; set; }

    /// <summary>ضریب سقف برآورد (مثلا 1.35) برای بازه بالا</summary>
    public decimal UpperBoundMultiplier { get; set; } = 1.35m;

    [MaxLength(1000)]
    public string? Disclaimer { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<EstimatorMaterialTier> MaterialTiers { get; set; } = new List<EstimatorMaterialTier>();
    public ICollection<EstimatorCityMultiplier> CityMultipliers { get; set; } = new List<EstimatorCityMultiplier>();
}
