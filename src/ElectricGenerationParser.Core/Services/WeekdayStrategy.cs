using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public class WeekdayStrategy : IRateStrategy
{
    public RateType? DetermineRate(DateTime timestamp, PeakPeriod weekdayPeak)
    {
        // Must be a weekday
        if (timestamp.DayOfWeek == DayOfWeek.Saturday || timestamp.DayOfWeek == DayOfWeek.Sunday)
        {
            return null;
        }

        int hour = timestamp.Hour;
        // E.g. 7am-7pm (Start=7, End=19) means [07:00, 19:00).
        if (hour >= weekdayPeak.StartHour && hour < weekdayPeak.EndHour)
        {
            return RateType.OnPeak;
        }

        // Weekday outside peak hours is OffPeak
        return RateType.OffPeak;
    }
}
