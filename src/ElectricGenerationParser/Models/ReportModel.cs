namespace ElectricGenerationParser.Models;

public class ReportModel
{
    public Dictionary<RateType, MetricSummary> Summaries { get; set; } = new();
    
    // Grand Totals across all rates
    public MetricSummary GrandTotal { get; set; } = new();

    // FR-18: Aggregated totals for Weekends
    public MetricSummary WeekendTotal { get; set; } = new();

    // FR-19: Individual breakdowns for each Holiday
    // Using DateOnly as key to easily sort by date and distinguish same holiday across years
    public SortedDictionary<DateOnly, HolidayMetricSummary> HolidaySummaries { get; set; } = new();

    // Convenience properties for AC compliance
    public MetricSummary TotalOnPeak => Summaries.TryGetValue(RateType.OnPeak, out var summary) ? summary : new MetricSummary();
    public MetricSummary TotalOffPeak => Summaries.TryGetValue(RateType.OffPeak, out var summary) ? summary : new MetricSummary();
}

public class MetricSummary
{
    public decimal Produced { get; set; }
    public decimal Consumed { get; set; }
    public decimal Export { get; set; }
    public decimal Import { get; set; }

    public void Add(GenerationRecord record)
    {
        Produced += record.Produced;
        Consumed += record.Consumed;
        Export += record.Export;
        Import += record.Import;
    }

    public void Add(decimal produced, decimal consumed, decimal export, decimal import)
    {
        Produced += produced;
        Consumed += consumed;
        Export += export;
        Import += import;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MetricSummary other) return false;
        // Using a small epsilon for decimal comparison if needed, but decimal is exact for currency/finite values usually.
        // However, let's play safe with simple equality for now.
        return Produced == other.Produced &&
               Consumed == other.Consumed &&
               Export == other.Export &&
               Import == other.Import;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Produced, Consumed, Export, Import);
    }
}
