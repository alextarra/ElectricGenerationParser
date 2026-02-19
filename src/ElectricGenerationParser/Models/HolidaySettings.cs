namespace ElectricGenerationParser.Models;

public class HolidaySettings
{
    public List<FixedHoliday> FixedHolidays { get; set; } = new();
    public List<FloatingHoliday> FloatingHolidays { get; set; } = new();
    public bool ObserveWeekendHolidays { get; set; } = true;
}

public class FixedHoliday
{
    public string Name { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Day { get; set; }
}

public class FloatingHoliday
{
    public string Name { get; set; } = string.Empty;
    public int Month { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int WeekInstance { get; set; } // 1=First, -1=Last, etc.
}
