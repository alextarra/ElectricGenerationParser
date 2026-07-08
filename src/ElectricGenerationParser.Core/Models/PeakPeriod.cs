namespace ElectricGenerationParser.Core.Models;

/// <summary>
/// A daily On-Peak window, e.g. [StartHour, EndHour). Derived from the selected
/// <see cref="TimeOfUsePlan"/>.
/// </summary>
public class PeakPeriod
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}
