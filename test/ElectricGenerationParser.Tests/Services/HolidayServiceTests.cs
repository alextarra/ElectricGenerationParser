using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;
using Microsoft.Extensions.Options;

namespace ElectricGenerationParser.Tests.Services;

public class HolidayServiceTests
{
    [Fact]
    public void GetHolidays_ShouldReturnEmptyList_WhenNoHolidaysConfigured()
    {
        // Arrange
        var settings = new HolidaySettings();
        var service = new HolidayService(Options.Create(settings));

        // Act
        var result = service.GetHolidays(2026);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetHolidays_ShouldReturnFixedHolidays()
    {
        // Arrange
        var settings = new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday>
            {
                new() { Name = "Christmas", Month = 12, Day = 25 }
            }
        };
        var service = new HolidayService(Options.Create(settings));

        // Act
        var result = service.GetHolidays(2026);

        // Assert
        Assert.Contains(new DateOnly(2026, 12, 25), result);
    }

    [Fact]
    public void GetHolidays_ShouldReturnFloatingHoliday()
    {
        // Arrange: Memorial Day (Last Monday of May)
        var settings = new HolidaySettings
        {
            FloatingHolidays = new List<FloatingHoliday>
            {
                new() { Name = "Memorial Day", Month = 5, DayOfWeek = DayOfWeek.Monday, WeekInstance = -1 }
            }
        };
        var service = new HolidayService(Options.Create(settings));

        // Act
        var result = service.GetHolidays(2026);

        // Assert: Memorial Day 2026 is May 25
        Assert.Contains(new DateOnly(2026, 5, 25), result);
    }

    [Fact]
    public void GetHolidays_ShouldObserveWeekendHoliday()
    {
        // Arrange: Christmas 2022 is Sunday. Observed on Monday Dec 26.
        var settings = new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday>
            {
                new() { Name = "Christmas", Month = 12, Day = 25 }
            },
            ObserveWeekendHolidays = true
        };
        var service = new HolidayService(Options.Create(settings));

        // Act
        var result = service.GetHolidays(2022);

        // Assert
        Assert.Contains(new DateOnly(2022, 12, 26), result);
    }

    [Fact]
    public void GetHolidays_IntegrationCheck_2026()
    {
        // 2026:
        // Jan 1: Thu
        // Memorial: May 25 (Mon)
        // July 4: Sat (Obs Fri July 3)
        // Labor: Sep 7 (Mon)
        // Thanksgiving: Nov 26 (Thu)
        // Christmas: Dec 25 (Fri)

        var settings = new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday>
            {
                new() { Name = "New Year's", Month = 1, Day = 1 },
                new() { Name = "Independence Day", Month = 7, Day = 4 },
                new() { Name = "Christmas", Month = 12, Day = 25 }
            },
            FloatingHolidays = new List<FloatingHoliday>
            {
                new() { Name = "Memorial Day", Month = 5, DayOfWeek = DayOfWeek.Monday, WeekInstance = -1 },
                new() { Name = "Labor Day", Month = 9, DayOfWeek = DayOfWeek.Monday, WeekInstance = 1 },
                new() { Name = "Thanksgiving", Month = 11, DayOfWeek = DayOfWeek.Thursday, WeekInstance = 4 }
            },
            ObserveWeekendHolidays = true
        };
        var service = new HolidayService(Options.Create(settings));

        var result = service.GetHolidays(2026);

        // Fixed
        Assert.Contains(new DateOnly(2026, 1, 1), result); // Thu
        Assert.Contains(new DateOnly(2026, 7, 4), result); // Sat
        Assert.Contains(new DateOnly(2026, 12, 25), result); // Fri

        // Observed
        Assert.Contains(new DateOnly(2026, 7, 3), result); // Fri (Observed Indep Day)

        // Floating
        Assert.Contains(new DateOnly(2026, 5, 25), result); // Memorial
        Assert.Contains(new DateOnly(2026, 9, 7), result);  // Labor
        Assert.Contains(new DateOnly(2026, 11, 26), result); // Thanksgiving
    }

    [Fact]
    public void GetHolidays_ShouldThrowInvalidOperationException_WhenFloatingHolidayConfigIsInvalid()
    {
        var settings = new HolidaySettings
        {
            FloatingHolidays = new List<FloatingHoliday>
            {
                new() { Name = "Invalid Holiday", Month = 13, DayOfWeek = DayOfWeek.Monday, WeekInstance = 1 }
            }
        };

        var service = new HolidayService(Options.Create(settings));

        var exception = Assert.Throws<InvalidOperationException>(() => service.GetHolidays(2026).ToList());
        Assert.Contains("Invalid floating holiday configuration", exception.Message);
    }

    [Fact]
    public void GetHolidayName_ShouldReturnCorrectName()
    {
        var settings = new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday> { new() { Name = "Christmas", Month = 12, Day = 25 } }
        };
        var service = new HolidayService(Options.Create(settings));

        var name = service.GetHolidayName(new DateOnly(2026, 12, 25));
        Assert.Equal("Christmas", name);
    }

    [Fact]
    public void GetHolidayName_ShouldReturnObservedName()
    {
        // Dec 25, 2022 is a Sunday. If observed, it should be Mon Dec 26.
        var settings = new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday> { new() { Name = "Christmas", Month = 12, Day = 25 } },
            ObserveWeekendHolidays = true
        };
        var service = new HolidayService(Options.Create(settings));

        var name = service.GetHolidayName(new DateOnly(2022, 12, 26)); 
        Assert.Equal("Christmas (Observed)", name);
    }

    [Fact]
    public void GetHolidayName_ShouldReturnNull_ForNonHoliday()
    {
        var settings = new HolidaySettings();
        var service = new HolidayService(Options.Create(settings));

        var name = service.GetHolidayName(new DateOnly(2026, 1, 1));
        Assert.Null(name);
    }
}
