namespace ElectricGenerationParser.Core.Models;

public class PeakHoursSettings
{
    public Dictionary<DayOfWeek, PeakPeriod> DailySchedules { get; set; } = new();
}

public class PeakPeriod
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}
