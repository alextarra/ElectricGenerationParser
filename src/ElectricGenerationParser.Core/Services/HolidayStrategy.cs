using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public class HolidayStrategy : IRateStrategy
{
    private readonly IHolidayService _holidayService;

    public HolidayStrategy(IHolidayService holidayService)
    {
        _holidayService = holidayService ?? throw new ArgumentNullException(nameof(holidayService));
    }

    public RateType? DetermineRate(DateTime timestamp)
    {
        if (_holidayService.IsHoliday(DateOnly.FromDateTime(timestamp)))
        {
            return RateType.OffPeak;
        }
        return null;
    }
}
