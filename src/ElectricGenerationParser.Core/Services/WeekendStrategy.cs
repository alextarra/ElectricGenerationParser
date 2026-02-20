using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public class WeekendStrategy : IRateStrategy
{
    public RateType? DetermineRate(DateTime timestamp)
    {
        if (timestamp.DayOfWeek == DayOfWeek.Saturday || timestamp.DayOfWeek == DayOfWeek.Sunday)
        {
            return RateType.OffPeak;
        }
        return null; // Not me, pass to next
    }
}
