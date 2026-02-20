using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Services;
using ElectricGenerationParser.Core.Services;
using ElectricGenerationParser.Core.Extensions;
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

    // Customize configuration loading:
    // In dev, appsettings.json is loaded by default.
    // In published build, appsettings.json is renamed to [AssemblyName].json.
    // We attempt to load [AssemblyName].json if it exists.
    string assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "appsettings";
    string uniqueConfigName = $"{assemblyName}.json";
    
    // Add the specific config file if found.
    // This handles the published scenario where appsettings.json is renamed.
    if (File.Exists(Path.Combine(AppContext.BaseDirectory, uniqueConfigName)))
    {
        builder.Configuration.AddJsonFile(uniqueConfigName, optional: false, reloadOnChange: true);
    }
    // Note: Host.CreateApplicationBuilder automatically attempts to load appsettings.json.
    // So in Dev environment, it will load appsettings.json naturally.

    // Register Core Services (Configuration, Models, Logic)
    builder.Services.AddElectricGenerationCore(builder.Configuration);

    // Register CLI-specific Services
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
