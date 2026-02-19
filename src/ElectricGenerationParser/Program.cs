using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;
using ElectricGenerationParser;

// Entry point
try 
{
    if (args.Length == 0)
    {
        Console.WriteLine("Usage: ElectricGenerationParser <path-to-csv>");
        return;
    }

    string filePath = args[0];

    var builder = Host.CreateApplicationBuilder(args);
    builder.Configuration.SetBasePath(AppContext.BaseDirectory);

    // Configuration
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    builder.Services.Configure<HolidaySettings>(
        builder.Configuration.GetSection("Holiday"));
    builder.Services.Configure<PeakHoursSettings>(
        builder.Configuration.GetSection("PeakHours"));

    // Services
    builder.Services.AddSingleton<IHolidayService, HolidayService>();
    builder.Services.AddTransient<ICsvParserService, CsvParserService>();

    // Register Strategies in Specific Order for RateCalculator
    // 1. Holiday (Precedence over all)
    builder.Services.AddSingleton<IRateStrategy, HolidayStrategy>();
    // 2. Weekend (Precedence over Weekday)
    builder.Services.AddSingleton<IRateStrategy, WeekendStrategy>();
    // 3. Weekday (Lowest Precedence, default logic)
    builder.Services.AddSingleton<IRateStrategy, WeekdayStrategy>();

    builder.Services.AddSingleton<IRateCalculator, RateCalculator>();
    builder.Services.AddTransient<IReportGenerator, ReportGenerator>();
    builder.Services.AddTransient<IConsoleService, ConsoleService>();
    builder.Services.AddSingleton<Application>();

    using IHost host = builder.Build();

    var app = host.Services.GetRequiredService<Application>();
    app.Run(filePath);
}
catch (Exception ex)
{
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"An error occurred: {ex.Message}");
    Console.ForegroundColor = originalColor;
    Environment.Exit(1);
}
