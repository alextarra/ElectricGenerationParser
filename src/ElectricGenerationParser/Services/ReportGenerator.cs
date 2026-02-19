using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ElectricGenerationParser.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq

namespace ElectricGenerationParser.Services;

public interface IReportGenerator
{
    ReportModel GenerateReport(List<GenerationRecord> records);
}

public class ReportGenerator : IReportGenerator
{
    private readonly IRateCalculator _rateCalculator;
    private readonly IHolidayService _holidayService;

    public ReportGenerator(IRateCalculator rateCalculator, IHolidayService holidayService)
    {
        _rateCalculator = rateCalculator ?? throw new ArgumentNullException(nameof(rateCalculator));
        _holidayService = holidayService ?? throw new ArgumentNullException(nameof(holidayService));
    }

    public ReportModel GenerateReport(List<GenerationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var report = new ReportModel();
        // Initialize summaries for known RateTypes to avoid nulls
        foreach (RateType rate in Enum.GetValues(typeof(RateType)))
        {
            report.Summaries[rate] = new MetricSummary();
        }

        foreach (var record in records)
        {
            // Populate calculated fields locally to avoid side effects on input list
            decimal export = 0;
            decimal import = 0;
            
            // Net Energy Metering (NEM) Logic:
            var net = record.Produced - record.Consumed;
            if (net > 0)
            {
                export = net;
                import = 0;
            }
            else
            {
                export = 0;
                import = -net; // Make positive
            }

            // Determine Rate
            var rateType = _rateCalculator.CalculateRate(record.Timestamp);

            // Add to bucket
            if (!report.Summaries.ContainsKey(rateType))
            {
                report.Summaries[rateType] = new MetricSummary();
            }
            report.Summaries[rateType].Add(record.Produced, record.Consumed, export, import);

            // Add to Grand Total
            report.GrandTotal.Add(record.Produced, record.Consumed, export, import);

            // Add to Weekend Total
            if (record.Timestamp.DayOfWeek == DayOfWeek.Saturday || record.Timestamp.DayOfWeek == DayOfWeek.Sunday)
            {
                report.WeekendTotal.Add(record.Produced, record.Consumed, export, import);
            }

            // Add to Holiday Summary
            var date = DateOnly.FromDateTime(record.Timestamp);
            var holidayName = _holidayService.GetHolidayName(date);
            if (!string.IsNullOrEmpty(holidayName))
            {
                if (!report.HolidaySummaries.ContainsKey(date))
                {
                    report.HolidaySummaries[date] = new HolidayMetricSummary { Name = holidayName };
                }
                report.HolidaySummaries[date].Add(record.Produced, record.Consumed, export, import);
            }
        }

        ValidateChecksums(report);

        return report;
    }

    internal void ValidateChecksums(ReportModel report)
    {
        var summedTotal = new MetricSummary();
        foreach (var summary in report.Summaries.Values)
        {
            summedTotal.Produced += summary.Produced;
            summedTotal.Consumed += summary.Consumed;
            summedTotal.Export += summary.Export;
            summedTotal.Import += summary.Import;
        }

        if (!summedTotal.Equals(report.GrandTotal))
        {
            throw new ElectricGenerationParser.Exceptions.ValidationException("Data Integrity Error: Grand Total does not match sum of Rate Types.");
        }
    }
}
