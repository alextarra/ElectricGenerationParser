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

    public Application(
        ILogger<Application> logger,
        IHolidayService holidayService,
        IRateCalculator rateCalculator)
    {
        _logger = logger;
        _holidayService = holidayService;
        _rateCalculator = rateCalculator;
    }

    public void Run(string inputFilePath)
    {
        _logger.LogInformation("Starting application with input file: {FilePath}", inputFilePath);

        if (!File.Exists(inputFilePath))
        {
            _logger.LogError("File not found: {FilePath}", inputFilePath);
            return;
        }

        // Placeholder for future logic
        _logger.LogInformation("File found. Processing logic to be implemented.");
    }
}
