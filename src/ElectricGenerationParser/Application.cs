using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Services;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser;

public interface IApplication
{
    void Run(string inputFilePath, ReportRequest request);
}

public class Application : IApplication
{
    private readonly ILogger<Application> _logger;
    private readonly ICsvParserService _csvParserService;
    private readonly IReportGenerator _reportGenerator;
    private readonly IConsoleService _consoleService;

    public Application(
        ILogger<Application> logger,
        ICsvParserService csvParserService,
        IReportGenerator reportGenerator,
        IConsoleService consoleService)
    {
        _logger = logger;
        _csvParserService = csvParserService;
        _reportGenerator = reportGenerator;
        _consoleService = consoleService;
    }

    public void Run(string inputFilePath, ReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("Starting application with input file: {FilePath}", inputFilePath);

        if (!File.Exists(inputFilePath))
        {
            // Throw exception to be handled by global handler for consistent experience
            throw new FileNotFoundException("Input file not found.", inputFilePath);
        }
        
        // Story 2.2: Parse CSV
        _logger.LogInformation("Parsing CSV file...");
        var records = _csvParserService.Parse(inputFilePath);
        _logger.LogInformation("Successfully parsed {Count} records.", records.Count);
        
        // Story 3.1 & 3.2: Generate and Render Report
        _logger.LogInformation(
            "Generating Report (Plan: {Plan}, Cutoff: {Cutoff}, Window: {From} to {To})...",
            request.Plan, request.CutoffHour,
            request.FromDate?.ToString() ?? "(all)", request.ToDate?.ToString() ?? "(all)");
        var report = _reportGenerator.GenerateReport(records, request);

        _consoleService.RenderReport(report);
    }
}
