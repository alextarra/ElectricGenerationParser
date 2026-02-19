using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;
using Microsoft.Extensions.Options;

namespace ElectricGenerationParser.Tests.Services;

public class WeekdayStrategyTests
{
    private readonly PeakHoursSettings _settings;

    public WeekdayStrategyTests()
    {
        _settings = new PeakHoursSettings();
        // Configure Mon-Fri 7am to 7pm (19)
        var peak = new PeakPeriod { StartHour = 7, EndHour = 19 };
        _settings.DailySchedules[DayOfWeek.Monday] = peak;
        _settings.DailySchedules[DayOfWeek.Tuesday] = peak;
        _settings.DailySchedules[DayOfWeek.Wednesday] = peak;
        _settings.DailySchedules[DayOfWeek.Thursday] = peak;
        _settings.DailySchedules[DayOfWeek.Friday] = peak;
    }

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForWeekend()
    {
        var strategy = new WeekdayStrategy(Options.Create(_settings));
        Assert.Null(strategy.DetermineRate(new DateTime(2026, 2, 21))); // Sat
    }

    [Fact]
    public void DetermineRate_ShouldReturnOnPeak_DuringPeakHours()
    {
        var strategy = new WeekdayStrategy(Options.Create(_settings));
        // Monday 1pm
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 13, 0, 0)));
        // Monday 7am (start inclusive)
        Assert.Equal(RateType.OnPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 7, 0, 0)));
    }

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_OutsidePeakHours()
    {
        var strategy = new WeekdayStrategy(Options.Create(_settings));
        // Monday 6am
        Assert.Equal(RateType.OffPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 6, 0, 0)));
        // Monday 7pm (end exclusive if my logic holds: 19:00 is off peak)
        Assert.Equal(RateType.OffPeak, strategy.DetermineRate(new DateTime(2026, 2, 23, 19, 0, 0)));
    }
}
