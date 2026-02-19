using ElectricGenerationParser.Models;

namespace ElectricGenerationParser.Services;

public class ConsoleService : IConsoleService
{
    private const string Format = "N2";
    private const int MetricWidth = 20;
    private const int ValueWidth = 15;

    public void RenderReport(ReportModel model)
    {
        // Header
        Console.WriteLine($"{PadText("Metric", MetricWidth)} {PadNumberHeader("On-Peak", ValueWidth)} {PadNumberHeader("Off-Peak", ValueWidth)} {PadNumberHeader("Total", ValueWidth)}");
        Console.WriteLine(new string('-', MetricWidth + (ValueWidth * 3) + 3));

        // Rows
        PrintRow("Produced", model.TotalOnPeak.Produced, model.TotalOffPeak.Produced, model.GrandTotal.Produced);
        PrintRow("Consumed", model.TotalOnPeak.Consumed, model.TotalOffPeak.Consumed, model.GrandTotal.Consumed);
        PrintRow("Export", model.TotalOnPeak.Export, model.TotalOffPeak.Export, model.GrandTotal.Export);
        PrintRow("Import", model.TotalOnPeak.Import, model.TotalOffPeak.Import, model.GrandTotal.Import);
    }

    private void PrintRow(string metric, decimal onPeak, decimal offPeak, decimal total)
    {
        Console.WriteLine($"{PadText(metric, MetricWidth)} {PadNumber(onPeak, ValueWidth)} {PadNumber(offPeak, ValueWidth)} {PadNumber(total, ValueWidth)}");
    }

    private string PadText(string text, int width)
    {
        return text.PadRight(width);
    }

    private string PadNumberHeader(string text, int width)
    {
        return text.PadLeft(width);
    }
    
    private string PadNumber(decimal value, int width)
    {
        var formatted = value.ToString(Format);
        // If the value is too long, we can't do much without breaking alignment.
        // We will return it as is, which will push the columns to the right but preserve the data.
        return formatted.Length > width ? formatted : formatted.PadLeft(width);
    }
}
