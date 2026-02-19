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
    private readonly IHolidayService _holidayService;
    private readonly IRateCalculator _rateCalculator;
    private readonly ICsvParserService _csvParserService;

    public Application(
        ILogger<Application> logger,
        IHolidayService holidayService,
        IRateCalculator rateCalculator,
        ICsvParserService csvParserService)
    {
        _logger = logger;
        _holidayService = holidayService;
        _rateCalculator = rateCalculator;
        _csvParserService = csvParserService;
    }

    public void Run(string inputFilePath)
    {
        _logger.LogInformation("Starting application with input file: {FilePath}", inputFilePath);

        if (!File.Exists(inputFilePath))
        {
            _logger.LogError("File not found: {FilePath}", inputFilePath);
            return;
        }
        
        try
        {
            // Story 2.2: Parse CSV
            _logger.LogInformation("Parsing CSV file...");
            var records = _csvParserService.Parse(inputFilePath);
            _logger.LogInformation("Successfully parsed {Count} records.", records.Count);
            
            // Further processing will happen in Epic 3
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to parse CSV file.");
            // In a real app we might want to exit with a non-zero code here, but Application.Run is void.
            // Exception will be logged.
        }
    }
}
