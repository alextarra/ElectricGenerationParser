namespace ElectricGenerationParser.Core.Models;

/// <summary>
/// Parameters that control how a report is generated: the mid-day-to-mid-day
/// reporting window and the Time-of-Use plan that defines the weekday On-Peak hours.
/// </summary>
public class ReportRequest
{
    /// <summary>
    /// Inclusive start date of the reporting window. When null (together with <see cref="ToDate"/>),
    /// all records are included with no date filtering.
    /// </summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>
    /// Inclusive end date of the reporting window. When null, all records are included.
    /// </summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>
    /// Hour (0-24) at which each day is split. The reporting window runs from
    /// <see cref="FromDate"/> at this hour up to (but not including) <see cref="ToDate"/> at this hour,
    /// so the first and last calendar days are each half-included ("mid-day to mid-day").
    /// The extremes are whole-day boundaries: 0 includes the From date and excludes the To date,
    /// while 24 (end-of-day midnight) excludes the From date and includes the To date.
    /// </summary>
    public int CutoffHour { get; set; } = 12;

    /// <summary>The Time-of-Use plan that sets the weekday On-Peak window.</summary>
    public TimeOfUsePlan Plan { get; set; } = TimeOfUsePlan.SevenToSeven;

    /// <summary>True when both dates are set and the report should be limited to the window.</summary>
    public bool HasDateWindow => FromDate.HasValue && ToDate.HasValue;
}
