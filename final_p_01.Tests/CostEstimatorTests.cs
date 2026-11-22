using final_p_01.Application.Services;

namespace final_p_01.Tests;

public class CostEstimatorTests
{
    private static EstimatorInput Input(int courtCount = 2, byte courtType = 1, string? city = "تهران", string? tier = null) =>
        new(courtType, courtCount, city, tier);

    private static readonly IReadOnlyDictionary<string, decimal> Cities = new Dictionary<string, decimal>
    {
        ["تهران"] = 1.2m,
        ["اصفهان"] = 1.0m
    };

    private static readonly IReadOnlyDictionary<string, decimal> Materials = new Dictionary<string, decimal>
    {
        ["استاندارد"] = 1.0m,
        ["پریمیوم"] = 1.5m
    };

    [Fact]
    public void Outdoor_TwoCourts_Tehran_AppliesCityMultiplier()
    {
        var result = CostEstimator.Calculate(Input(courtCount: 2), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null);

        Assert.NotNull(result);
        Assert.Equal(1_200_000, result.PricePerCourt);   // 1,000,000 × 1.2
        Assert.Equal(2_400_000, result.MinCost);         // × 2 courts
        Assert.Equal(3_000_000, result.MaxCost);         // × 1.25 upper bound
    }

    [Fact]
    public void Indoor_UsesIndoorBasePrice()
    {
        var result = CostEstimator.Calculate(Input(courtCount: 1, courtType: 2, city: "اصفهان", tier: "پریمیوم"),
            1_000_000, 2_000_000, 1.1m, Cities, Materials, null);

        Assert.NotNull(result);
        Assert.Equal(3_000_000, result.PricePerCourt);   // 2,000,000 × 1.0 city × 1.5 material
    }

    [Fact]
    public void OneCourt_IsMinimumValidCount()
    {
        Assert.NotNull(CostEstimator.Calculate(Input(courtCount: 1), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null));
    }

    [Fact]
    public void FiftyCourts_IsMaximumValidCount()
    {
        var result = CostEstimator.Calculate(Input(courtCount: 50), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null);
        Assert.NotNull(result);
        Assert.Equal(50, result.MinCost / result.PricePerCourt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void CourtCountOutsideRange_ReturnsNull(int courtCount)
    {
        Assert.Null(CostEstimator.Calculate(Input(courtCount: courtCount), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null));
    }

    [Fact]
    public void UnknownCity_ReturnsNull()
    {
        Assert.Null(CostEstimator.Calculate(Input(city: "شیراز"), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null));
    }

    [Fact]
    public void UnknownMaterialTier_ReturnsNull()
    {
        Assert.Null(CostEstimator.Calculate(Input(tier: "طلایی"), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void UnknownCourtType_ReturnsNull(byte courtType)
    {
        Assert.Null(CostEstimator.Calculate(Input(courtType: courtType), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null));
    }

    [Fact]
    public void MissingCityAndTier_DefaultToUnityMultiplier()
    {
        var result = CostEstimator.Calculate(Input(courtCount: 1, city: null, tier: null), 1_000_000, 1_500_000, 1.25m, Cities, Materials, null);

        Assert.NotNull(result);
        Assert.Equal(1_000_000, result.PricePerCourt);
    }
}
