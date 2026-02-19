using ElectricGenerationParser.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricGenerationParser.Services;

public class HolidayService : IHolidayService
{
    private readonly HolidaySettings _settings;
    private readonly ConcurrentDictionary<int, Dictionary<DateOnly, string>> _holidaysCache = new();

    public HolidayService(IOptions<HolidaySettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IEnumerable<DateOnly> GetHolidays(int year)
    {
        return _holidaysCache.GetOrAdd(year, BuildHolidays).Keys;
    }
    
    public bool IsHoliday(DateOnly date)
    {
        return _holidaysCache.GetOrAdd(date.Year, BuildHolidays).ContainsKey(date);
    }

    public string? GetHolidayName(DateOnly date)
    {
        var holidays = _holidaysCache.GetOrAdd(date.Year, BuildHolidays);
        return holidays.TryGetValue(date, out var name) ? name : null;
    }

    private Dictionary<DateOnly, string> BuildHolidays(int year)
    {
        var holidays = new Dictionary<DateOnly, string>();

        foreach (var fixedHoliday in _settings.FixedHolidays)
        {
            try
            {
                var date = new DateOnly(year, fixedHoliday.Month, fixedHoliday.Day);
                holidays.TryAdd(date, fixedHoliday.Name);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Date doesn't exist in this year
            }
        }

        foreach (var floating in _settings.FloatingHolidays)
        {
            var date = CalculateFloatingHoliday(year, floating);
            if (date.HasValue)
            {
                holidays.TryAdd(date.Value, floating.Name);
            }
        }

        if (_settings.ObserveWeekendHolidays)
        {
            var observedUpdates = new Dictionary<DateOnly, string>();
            foreach (var kvp in holidays)
            {
                var date = kvp.Key;
                var name = kvp.Value;
                
                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    var observedDate = date.AddDays(-1);
                    // Avoid overwriting if another holiday exists there, though rare
                    if (!holidays.ContainsKey(observedDate))
                    {
                        observedUpdates.TryAdd(observedDate, $"{name} (Observed)");
                    }
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    var observedDate = date.AddDays(1);
                    if (!holidays.ContainsKey(observedDate))
                    {
                        observedUpdates.TryAdd(observedDate, $"{name} (Observed)");
                    }
                }
            }
            
            foreach (var kvp in observedUpdates)
            {
                holidays.TryAdd(kvp.Key, kvp.Value);
            }
        }

        return holidays;
    }

    private DateOnly? CalculateFloatingHoliday(int year, FloatingHoliday config)
    {
        try 
        {
            var daysInMonth = DateTime.DaysInMonth(year, config.Month);
            var firstDayOfMonth = new DateOnly(year, config.Month, 1);
            
            var dates = new List<DateOnly>();
            for (int i = 0; i < daysInMonth; i++)
            {
                var d = firstDayOfMonth.AddDays(i);
                if (d.DayOfWeek == config.DayOfWeek)
                {
                    dates.Add(d);
                }
            }

            if (config.WeekInstance > 0)
            {
                if (config.WeekInstance <= dates.Count)
                {
                    return dates[config.WeekInstance - 1];
                }
            }
            else if (config.WeekInstance < 0)
            {
                int index = dates.Count + config.WeekInstance;
                if (index >= 0 && index < dates.Count)
                {
                    return dates[index];
                }
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException(
                $"Invalid floating holiday configuration for '{config.Name}' in month {config.Month} with week instance {config.WeekInstance}.",
                ex);
        }
        return null;
    }
}

