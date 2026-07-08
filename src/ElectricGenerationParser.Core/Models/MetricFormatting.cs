namespace ElectricGenerationParser.Core.Models;

/// <summary>Display helpers for energy metrics.</summary>
public static class MetricFormatting
{
    /// <summary>
    /// Formats a raw watt-hour value as kilowatt-hours: divides by 1000 and shows up to
    /// three decimal places without rounding (Wh sums are whole numbers, so /1000 is exact
    /// to at most three decimals). Trailing zeros are dropped, e.g. 46000 -> "46",
    /// 181367 -> "181.367", 2479444 -> "2,479.444".
    /// </summary>
    public static string ToKwh(this decimal wattHours) => (wattHours / 1000m).ToString("#,##0.###");
}
