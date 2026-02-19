using ElectricGenerationParser.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ElectricGenerationParser.Services;

public class HolidayService : IHolidayService
{
    private readonly HolidaySettings _settings;
    private readonly ConcurrentDictionary<int, HashSet<DateOnly>> _holidaysCache = new();

    public HolidayService(IOptions<HolidaySettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IEnumerable<DateOnly> GetHolidays(int year)
    {
        return _holidaysCache.GetOrAdd(year, BuildHolidays);
    }

    private HashSet<DateOnly> BuildHolidays(int year)
    {
        var holidays = new HashSet<DateOnly>();

        foreach (var fixedHoliday in _settings.FixedHolidays)
        {
            try
            {
                holidays.Add(new DateOnly(year, fixedHoliday.Month, fixedHoliday.Day));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Date doesn't exist in this year (e.g. Feb 29 non-leap)
            }
        }

        foreach (var floating in _settings.FloatingHolidays)
        {
            var date = CalculateFloatingHoliday(year, floating);
            if (date.HasValue)
            {
                holidays.Add(date.Value);
            }
        }

        if (_settings.ObserveWeekendHolidays)
        {
            var observed = new List<DateOnly>();
            foreach (var h in holidays)
            {
                if (h.DayOfWeek == DayOfWeek.Saturday)
                {
                    observed.Add(h.AddDays(-1));
                }
                else if (h.DayOfWeek == DayOfWeek.Sunday)
                {
                    observed.Add(h.AddDays(1));
                }
            }
            foreach (var o in observed)
            {
                holidays.Add(o);
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

    public bool IsHoliday(DateOnly date)
    {
        return GetHolidays(date.Year).Contains(date);
    }
}
