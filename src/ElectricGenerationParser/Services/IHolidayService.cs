namespace ElectricGenerationParser.Services;

public interface IHolidayService
{
    IEnumerable<DateOnly> GetHolidays(int year);
    bool IsHoliday(DateOnly date);
    string? GetHolidayName(DateOnly date);
}
