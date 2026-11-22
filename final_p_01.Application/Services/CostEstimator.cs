using Darabico.Domain.Enums;

namespace final_p_01.Application.Services;

public record EstimatorInput(
    byte CourtTypeValue,
    int CourtCount,
    string? City,
    string? MaterialTierTitle);

public record EstimatorResult(
    long MinCost,
    long MaxCost,
    long PricePerCourt,
    string? City,
    string? MaterialTierTitle,
    string? Disclaimer);

/// <summary>
/// فرمول برآورد هزینه: قیمت پایه هر زمین × تعداد زمین × ضریب شهر × ضریب متریال.
/// بازه بالایی با ضریب سقف تنظیمات محاسبه می‌شود.
/// </summary>
public static class CostEstimator
{
    public static EstimatorResult? Calculate(
        EstimatorInput input,
        long basePricePerCourtOutdoor,
        long basePricePerCourtIndoor,
        decimal upperBoundMultiplier,
        IReadOnlyDictionary<string, decimal> cityMultipliers,
        IReadOnlyDictionary<string, decimal> materialMultipliers,
        string? disclaimer)
    {
        if (input.CourtCount < 1 || input.CourtCount > 50)
            return null;

        long basePrice = input.CourtTypeValue switch
        {
            (byte)CourtType.Indoor => basePricePerCourtIndoor,
            (byte)CourtType.Outdoor => basePricePerCourtOutdoor,
            _ => 0
        };

        if (basePrice <= 0)
            return null;

        decimal cityMultiplier = 1m;
        if (!string.IsNullOrWhiteSpace(input.City))
        {
            var key = input.City.Trim();
            if (!cityMultipliers.TryGetValue(key, out cityMultiplier))
                return null; // شهر پشتیبانی‌نشده: برآورد ارائه نمی‌شود
        }

        decimal materialMultiplier = 1m;
        if (!string.IsNullOrWhiteSpace(input.MaterialTierTitle))
        {
            var key = input.MaterialTierTitle.Trim();
            if (!materialMultipliers.TryGetValue(key, out materialMultiplier))
                return null;
        }

        decimal perCourt = basePrice * cityMultiplier * materialMultiplier;
        decimal min = perCourt * input.CourtCount;
        decimal max = min * upperBoundMultiplier;

        return new EstimatorResult(
            MinCost: (long)Math.Round(min, MidpointRounding.AwayFromZero),
            MaxCost: (long)Math.Round(max, MidpointRounding.AwayFromZero),
            PricePerCourt: (long)Math.Round(perCourt, MidpointRounding.AwayFromZero),
            City: input.City,
            MaterialTierTitle: input.MaterialTierTitle,
            Disclaimer: disclaimer);
    }
}
