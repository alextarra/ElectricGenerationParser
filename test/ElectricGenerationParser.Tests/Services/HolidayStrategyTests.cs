using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;

namespace ElectricGenerationParser.Tests.Services;

public class HolidayStrategyTests
{
    private class FakeHolidayService : IHolidayService
    {
        public IEnumerable<DateOnly> GetHolidays(int year) => new List<DateOnly>();
        public bool IsHoliday(DateOnly date) => date.Day == 1 && date.Month == 1; // New Year is Holiday
    }

    [Fact]
    public void DetermineRate_ShouldReturnOffPeak_ForHoliday()
    {
        var strategy = new HolidayStrategy(new FakeHolidayService());
        var result = strategy.DetermineRate(new DateTime(2026, 1, 1));
        Assert.Equal(RateType.OffPeak, result);
    }

    [Fact]
    public void DetermineRate_ShouldReturnNull_ForNonHoliday()
    {
        var strategy = new HolidayStrategy(new FakeHolidayService());
        var result = strategy.DetermineRate(new DateTime(2026, 1, 2));
        Assert.Null(result);
    }
}
