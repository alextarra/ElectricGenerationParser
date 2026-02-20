using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;

namespace ElectricGenerationParser.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElectricGenerationCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<HolidaySettings>(
            configuration.GetSection("Holiday"));
        services.Configure<PeakHoursSettings>(
            configuration.GetSection("PeakHours"));

        // Services
        services.AddSingleton<IHolidayService, HolidayService>();
        services.AddTransient<ICsvParserService, CsvParserService>();

        // Register Strategies in Specific Order for RateCalculator
        // 1. Holiday (Precedence over all)
        services.AddSingleton<IRateStrategy, HolidayStrategy>();
        // 2. Weekend (Precedence over Weekday)
        services.AddSingleton<IRateStrategy, WeekendStrategy>();
        // 3. Weekday (Default fallback for M-F)
        services.AddSingleton<IRateStrategy, WeekdayStrategy>();

        // Register Calculator (consumes all IRateStrategy)
        services.AddSingleton<IRateCalculator, RateCalculator>();
        
        // Register Report Generator
        services.AddSingleton<IReportGenerator, ReportGenerator>();

        return services;
    }
}
