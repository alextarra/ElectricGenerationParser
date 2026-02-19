using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;

namespace ElectricGenerationParser;

public interface IApplication
{
    void Run(string inputFilePath);
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

    public void Run(string inputFilePath)
    {
        _logger.LogInformation("Starting application with input file: {FilePath}", inputFilePath);

        if (!File.Exists(inputFilePath))
        {
            _logger.LogError("File not found: {FilePath}", inputFilePath);
            Environment.Exit(1);
            return;
        }
        
        try
        {
            // Story 2.2: Parse CSV
            _logger.LogInformation("Parsing CSV file...");
            var records = _csvParserService.Parse(inputFilePath);
            _logger.LogInformation("Successfully parsed {Count} records.", records.Count);
            
            // Story 3.1 & 3.2: Generate and Render Report
            _logger.LogInformation("Generating Report...");
            var report = _reportGenerator.GenerateReport(records);
            
            _consoleService.RenderReport(report);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to parse CSV file.");
            Environment.Exit(1);
        }
    }
}
