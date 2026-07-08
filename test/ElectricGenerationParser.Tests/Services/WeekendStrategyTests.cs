using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser.Tests.Services;

public class WeekendStrategyTests
{
    private static readonly PeakPeriod Peak = new() { StartHour = 7, EndHour = 19 };

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForSaturday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 21), Peak); // Sat
        Assert.Equal(RateType.OffPeak, result);
    }

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForSunday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 22), Peak); // Sun
        Assert.Equal(RateType.OffPeak, result);
    }

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForMonday()
    {
        var strategy = new WeekendStrategy();
        var result = strategy.DetermineRate(new DateTime(2026, 2, 23), Peak); // Mon
        Assert.Null(result);
    }
}
