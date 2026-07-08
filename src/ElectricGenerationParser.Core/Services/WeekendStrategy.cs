using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public class WeekendStrategy : IRateStrategy
{
    public RateType? DetermineRate(DateTime timestamp, PeakPeriod weekdayPeak)
    {
        if (timestamp.DayOfWeek == DayOfWeek.Saturday || timestamp.DayOfWeek == DayOfWeek.Sunday)
        {
            return RateType.OffPeak;
        }
        return null; // Not me, pass to next
    }
}
