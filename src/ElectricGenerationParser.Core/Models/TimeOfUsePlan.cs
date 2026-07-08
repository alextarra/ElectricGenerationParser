namespace ElectricGenerationParser.Core.Models;

/// <summary>
/// Time-of-Use rate plan choices. Each plan defines a 12-hour weekday On-Peak window.
/// </summary>
public enum TimeOfUsePlan
{
    /// <summary>On-Peak 7am - 7pm.</summary>
    SevenToSeven,

    /// <summary>On-Peak 8am - 8pm.</summary>
    EightToEight,

    /// <summary>On-Peak 9am - 9pm.</summary>
    NineToNine,

    /// <summary>On-Peak 10am - 10pm.</summary>
    TenToTen
}

public static class TimeOfUsePlanExtensions
{
    /// <summary>
    /// Maps a plan to its weekday On-Peak window. All plans span 12 hours.
    /// </summary>
    public static PeakPeriod ToPeakPeriod(this TimeOfUsePlan plan)
    {
        int startHour = plan switch
        {
            TimeOfUsePlan.SevenToSeven => 7,
            TimeOfUsePlan.EightToEight => 8,
            TimeOfUsePlan.NineToNine => 9,
            TimeOfUsePlan.TenToTen => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown Time-of-Use plan.")
        };

        return new PeakPeriod { StartHour = startHour, EndHour = startHour + 12 };
    }

    /// <summary>Human-friendly label, e.g. "7am - 7pm".</summary>
    public static string ToDisplayName(this TimeOfUsePlan plan) => plan switch
    {
        TimeOfUsePlan.SevenToSeven => "7am - 7pm",
        TimeOfUsePlan.EightToEight => "8am - 8pm",
        TimeOfUsePlan.NineToNine => "9am - 9pm",
        TimeOfUsePlan.TenToTen => "10am - 10pm",
        _ => plan.ToString()
    };
}
