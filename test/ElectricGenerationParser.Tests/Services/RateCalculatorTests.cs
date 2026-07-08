using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;
using Microsoft.Extensions.Options;

namespace ElectricGenerationParser.Tests.Services;

public class RateCalculatorTests
{
    private static readonly PeakPeriod Peak = new() { StartHour = 7, EndHour = 19 };

    private class MockMatchingStrategy : IRateStrategy
    {
        private readonly RateType? _result;
        public MockMatchingStrategy(RateType? result) => _result = result;
        public RateType? DetermineRate(DateTime timestamp, PeakPeriod weekdayPeak) => _result;
    }

    [Fact]
    public void CalculateRate_ShouldReturnResultOfFirstStrategyThatReturnsValue()
    {
        // Arrange
        var strategies = new List<IRateStrategy>
        {
            new MockMatchingStrategy(null),
            new MockMatchingStrategy(RateType.OnPeak),
            new MockMatchingStrategy(RateType.OffPeak)
        };

        var calculator = new RateCalculator(strategies);

        // Act
        var result = calculator.CalculateRate(new DateTime(2026, 1, 5, 10, 0, 0), Peak);

        // Assert
        Assert.Equal(RateType.OnPeak, result);
    }

    [Fact]
    public void CalculateRate_ShouldThrow_WhenNoStrategyMatches()
    {
        // Arrange
        var strategies = new List<IRateStrategy>
        {
            new MockMatchingStrategy(null),
            new MockMatchingStrategy(null)
        };
        var calculator = new RateCalculator(strategies);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => calculator.CalculateRate(new DateTime(2026, 1, 5, 10, 0, 0), Peak));
    }

    [Fact]
    public void CalculateRate_Integration_ShouldWorkWithRealStrategies()
    {
        // Integration test with real strategies

        // 1. Holiday Settings
        var holidaySettings = new HolidaySettings
        {
            FixedHolidays = new() { new FixedHoliday { Name="New Year", Month=1, Day=1 } }
        };
        var holidayService = new HolidayService(Options.Create(holidaySettings));

        // 2. Compose Strategies (weekday peak window is supplied per-call)
        var strategies = new List<IRateStrategy>
        {
            new HolidayStrategy(holidayService),
            new WeekendStrategy(),
            new WeekdayStrategy()
        };

        var calculator = new RateCalculator(strategies);

        // Test Cases (7am-7pm peak window):

        // Holiday (Jan 1 2026 is Thursday) -> Should be OffPeak (HolidayStrategy returns OffPeak)
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 1, 10, 0, 0), Peak));

        // Weekend (Jan 3 2026 is Saturday) -> Should be OffPeak (WeekendStrategy)
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 3, 10, 0, 0), Peak));

        // Weekday Peak (Jan 5 2026 is Monday, 10am) -> OnPeak
        Assert.Equal(RateType.OnPeak, calculator.CalculateRate(new DateTime(2026, 1, 5, 10, 0, 0), Peak));

        // Weekday OffPeak (Jan 5 2026) 8pm -> OffPeak
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 5, 20, 0, 0), Peak));
    }
}
