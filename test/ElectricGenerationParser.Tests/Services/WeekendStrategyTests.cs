using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser.Tests.Services;

public class WeekendStrategyTests
{
    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForSaturday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 21)); // Sat
        Assert.Equal(RateType.OffPeak, result);
    }
    
    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForSunday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 22)); // Sun
        Assert.Equal(RateType.OffPeak, result);
    }

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForMonday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 23)); // Mon
        Assert.Null(result);
    }
}
