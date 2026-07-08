using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public interface IRateStrategy
{
    /// <summary>
    /// Determines the rate for a timestamp, or null if this strategy does not apply.
    /// </summary>
    /// <param name="timestamp">The reading timestamp.</param>
    /// <param name="weekdayPeak">The weekday On-Peak window in effect for this report (from the selected Time-of-Use plan).</param>
    RateType? DetermineRate(DateTime timestamp, PeakPeriod weekdayPeak);
}
