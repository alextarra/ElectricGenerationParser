using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser.Tests.Services;

public class HolidayStrategyTests
{
    private class FakeHolidayService : IHolidayService
    {
        public IEnumerable<DateOnly> GetHolidays(int year) => new List<DateOnly>();
        public bool IsHoliday(DateOnly date) => date.Day == 1 && date.Month == 1; // New Year is Holiday
        public string? GetHolidayName(DateOnly date) => IsHoliday(date) ? "New Year's Day" : null;
    }

    private static readonly PeakPeriod Peak = new() { StartHour = 7, EndHour = 19 };

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForHoliday()
    {
        var strategy = new HolidayStrategy(new FakeHolidayService());
        var result = strategy.DetermineRate(new DateTime(2026, 1, 1), Peak);
        Assert.Equal(RateType.OffPeak, result);
    }

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForNonHoliday()
    {
        var strategy = new HolidayStrategy(new FakeHolidayService());
        var result = strategy.DetermineRate(new DateTime(2026, 1, 2), Peak);
        Assert.Null(result);
    }
}
