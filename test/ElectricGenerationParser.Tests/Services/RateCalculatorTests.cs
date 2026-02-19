using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;
using Microsoft.Extensions.Options;

namespace ElectricGenerationParser.Tests.Services;

public class RateCalculatorTests
{
    private class MockMatchingStrategy : IRateStrategy
    {
        private readonly RateType? _result;
        public MockMatchingStrategy(RateType? result) => _result = result;
        public RateType? DetermineRate(DateTime timestamp) => _result;
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
        var result = calculator.CalculateRate(DateTime.Now);

        // Assert
        Assert.Equal(RateType.OnPeak, result);
    }

    [Fact]
    public void CalculateRate_ShouldDefaultToOffPeak_WhenNoStrategyMatches()
    {
        // Arrange
        var strategies = new List<IRateStrategy>
        {
            new MockMatchingStrategy(null),
            new MockMatchingStrategy(null)
        };
        var calculator = new RateCalculator(strategies);

        // Act
        var result = calculator.CalculateRate(DateTime.Now);

        // Assert
        Assert.Equal(RateType.OffPeak, result);
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
        
        // 2. Peak Hours Settings
        var peakSettings = new PeakHoursSettings();
        // Configure standard 7-19 peak
        var peak = new PeakPeriod { StartHour=7, EndHour=19 };
        peakSettings.DailySchedules[DayOfWeek.Monday] = peak;
        peakSettings.DailySchedules[DayOfWeek.Tuesday] = peak;
        
        // 3. Compose Strategies
        var strategies = new List<IRateStrategy>
        {
            new HolidayStrategy(holidayService),
            new WeekendStrategy(),
            new WeekdayStrategy(Options.Create(peakSettings))
        };
        
        var calculator = new RateCalculator(strategies);

        // Test Cases:
        
        // Holiday (Jan 1 2026 is Thursday) -> Should be OffPeak (HolidayStrategy returns OffPeak)
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 1, 10, 0, 0)));
        
        // Weekend (Jan 3 2026 is Saturday) -> Should be OffPeak (WeekendStrategy)
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 3, 10, 0, 0)));
        
        // Weekday Peak (Jan 2 2026 is Friday, 10am) -> Assuming Fri is not configured above? Wait I only configured Mon/Tue.
        // Let's test Monday (Jan 5 2026) 10am -> OnPeak
        Assert.Equal(RateType.OnPeak, calculator.CalculateRate(new DateTime(2026, 1, 5, 10, 0, 0)));
        
        // Weekday OffPeak (Jan 5 2026) 8pm -> OffPeak
        Assert.Equal(RateType.OffPeak, calculator.CalculateRate(new DateTime(2026, 1, 5, 20, 0, 0)));
    }
}
