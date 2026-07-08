using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser.Tests.Services;

public class WeekdayStrategyTests
{
    // 7am - 7pm On-Peak window
    private static readonly PeakPeriod Peak = new() { StartHour = 7, EndHour = 19 };

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForWeekend()
    {
        var strategy = new WeekdayStrategy();
        Assert.Null(strategy.DetermineRate(new DateTime(2026, 2, 21), Peak)); // Sat
    }

    [Fact]
    public void DetermineRate_ShouldReturnOnPeak_DuringPeakHours()
    {
        var strategy = new WeekdayStrategy();
        // Monday 1pm
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 13, 0, 0), Peak));
        // Monday 7am (start inclusive)
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 7, 0, 0), Peak));
    }

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_OutsidePeakHours()
    {
        var strategy = new WeekdayStrategy();
        // Monday 6am
        Assert.Equal(RateType.OffPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 6, 0, 0), Peak));
        // Monday 7pm (end exclusive: 19:00 is off peak)
        Assert.Equal(RateType.OffPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 19, 0, 0), Peak));
    }

    [Fact]
    public void DetermineRate_ShouldRespectPlanPeakWindow()
    {
        var strategy = new WeekdayStrategy();
        // 10am - 10pm plan: 8pm should be On-Peak, whereas it would be Off-Peak under a 7am-7pm plan.
        var tenToTen = TimeOfUsePlan.TenToTen.ToPeakPeriod();
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 20, 0, 0), tenToTen));
        // 9am under the 10am-10pm plan is Off-Peak.
        Assert.Equal(RateType.OffPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 9, 0, 0), tenToTen));
    }

    [Fact]
    public void DetermineRate_ShouldHandleQuarterHourReadings()
    {
        var strategy = new WeekdayStrategy();
        // Monday 18:45 (last quarter before 7pm) is On-Peak under 7am-7pm.
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 18, 45, 0), Peak));
    }
}
