using ElectricGenerationParser.Models;
using Microsoft.Extensions.Options;

namespace ElectricGenerationParser.Services;

public class WeekdayStrategy : IRateStrategy
{
    private readonly PeakHoursSettings _settings;

    public WeekdayStrategy(IOptions<PeakHoursSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public RateType? DetermineRate(DateTime timestamp)
    {
        // Must be a weekday
        if (timestamp.DayOfWeek == DayOfWeek.Saturday || timestamp.DayOfWeek == DayOfWeek.Sunday)
        {
            return null;
        }

        if (_settings.DailySchedules.TryGetValue(timestamp.DayOfWeek, out var schedule))
        {
            int hour = timestamp.Hour;
            // E.g. 7am-7pm (Start=7, End=19) means [07:00, 19:00).
            if (hour >= schedule.StartHour && hour < schedule.EndHour)
            {
                return RateType.OnPeak;
            }
        }

        // Weekday outside peak hours is OffPeak
        return RateType.OffPeak;
    }
}
