using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;
using ElectricGenerationParser.Services;
using System.Text;

namespace ElectricGenerationParser.Tests.Services;

public class ConsoleServiceTests : IDisposable
{
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;
    private readonly ConsoleService _service;

    public ConsoleServiceTests()
    {
        _stringWriter = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_stringWriter);
        _service = new ConsoleService();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _stringWriter.Dispose();
    }

    [Fact]
    public void RenderReport_ShouldOutputCorrectTableStructure()
    {
        // Arrange
        var model = new ReportModel();
        model.Summaries[RateType.OnPeak] = new MetricSummary { Produced = 60, Consumed = 120, Export = 30, Import = 90 };
        model.Summaries[RateType.OffPeak] = new MetricSummary { Produced = 40, Consumed = 80, Export = 20, Import = 60 };
        model.GrandTotal = new MetricSummary { Produced = 100, Consumed = 200, Export = 50, Import = 150 };

        // Act
        _service.RenderReport(model);

        // Assert
        var output = _stringWriter.ToString();
        
        // Header
        Assert.Contains("Metric", output);
        Assert.Contains("On-Peak", output);
        Assert.Contains("Off-Peak", output);
        Assert.Contains("Total", output);

        // Data Rows
        Assert.Contains("Produced (kWh)", output);
        Assert.Contains("Consumed (kWh)", output);
        Assert.Contains("Exported to Grid (kWh)", output);
        Assert.Contains("Imported from Grid (kWh)", output);

        // Values shown in kWh (raw Wh / 1000, up to 3 decimals, not rounded)
        Assert.Contains("0.1", output);   // GrandTotal Produced 100 Wh
        Assert.Contains("0.06", output);  // On-Peak Produced 60 Wh
        Assert.Contains("0.04", output);  // Off-Peak Produced 40 Wh
    }

    [Fact]
    public void RenderReport_ShouldDisplayHolidayWithDate()
    {
        // Arrange
        var model = new ReportModel();
        // Setup RateType summaries so validation doesn't crash (though RenderReport doesn't validate)
        model.Summaries[RateType.OnPeak] = new MetricSummary();
        model.Summaries[RateType.OffPeak] = new MetricSummary();
        
        var date = new DateOnly(2026, 12, 25);
        model.HolidaySummaries[date] = new HolidayMetricSummary 
        { 
            Name = "Christmas", 
            Produced = 500 
        };

        // Act
        _service.RenderReport(model);

        // Assert
        var output = _stringWriter.ToString();
        Assert.Contains("Christmas (2026-12-25):", output);
        Assert.Contains("Produced:", output);
        Assert.Contains("0.5", output);
        Assert.Contains("Holiday Breakdowns (kWh)", output);
    }
}
