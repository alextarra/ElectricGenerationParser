using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Services;

public class ConsoleService : IConsoleService
{
    private const int MetricWidth = 30;
    private const int ValueWidth = 15;

    public void RenderReport(ReportModel model)
    {
        // Header
        Console.WriteLine($"{PadText("Metric", MetricWidth)} {PadNumberHeader("On-Peak", ValueWidth)} {PadNumberHeader("Off-Peak", ValueWidth)} {PadNumberHeader("Total", ValueWidth)}");
        Console.WriteLine(new string('-', MetricWidth + (ValueWidth * 3) + 3));

        // Rows
        PrintRow("Produced (kWh)", model.TotalOnPeak.Produced, model.TotalOffPeak.Produced, model.GrandTotal.Produced);
        PrintRow("Consumed (kWh)", model.TotalOnPeak.Consumed, model.TotalOffPeak.Consumed, model.GrandTotal.Consumed);
        PrintRow("Exported to Grid (kWh)", model.TotalOnPeak.Export, model.TotalOffPeak.Export, model.GrandTotal.Export);
        PrintRow("Imported from Grid (kWh)", model.TotalOnPeak.Import, model.TotalOffPeak.Import, model.GrandTotal.Import);
        PrintRow("Net Import (kWh)", model.TotalOnPeak.NetImport, model.TotalOffPeak.NetImport, model.GrandTotal.NetImport);

        // Weekend Totals
        Console.WriteLine();
        Console.WriteLine("Weekend Totals (kWh)");
        Console.WriteLine(new string('-', 20)); // Underline
        PrintSummary(model.WeekendTotal);

        // Holiday Breakdowns
        if (model.HolidaySummaries.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Holiday Breakdowns (kWh)");
            Console.WriteLine(new string('-', 20)); // Underline
            foreach (var kvp in model.HolidaySummaries)
            {
                var date = kvp.Key;
                var summary = kvp.Value;
                Console.WriteLine($"{summary.Name} ({date:yyyy-MM-dd}):");
                PrintSummary(summary, indent: "  ");
                Console.WriteLine();
            }
        }
    }
    
    private void PrintSummary(MetricSummary summary, string indent = "")
    {
        Console.WriteLine($"{indent}{PadText("Produced:", 20)} {summary.Produced.ToKwh()}");
        Console.WriteLine($"{indent}{PadText("Consumed:", 20)} {summary.Consumed.ToKwh()}");
        Console.WriteLine($"{indent}{PadText("Export:", 20)} {summary.Export.ToKwh()}");
        Console.WriteLine($"{indent}{PadText("Import:", 20)} {summary.Import.ToKwh()}");
        Console.WriteLine($"{indent}{PadText("Net Import:", 20)} {summary.NetImport.ToKwh()}");
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
        var formatted = value.ToKwh();
        // If the value is too long, we can't do much without breaking alignment.
        // We will return it as is, which will push the columns to the right but preserve the data.
        return formatted.Length > width ? formatted : formatted.PadLeft(width);
    }
}
