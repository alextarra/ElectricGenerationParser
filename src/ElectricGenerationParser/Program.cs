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
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        PrintUsage();
        return;
    }

    string filePath = args[0];
    ReportRequest request = ParseReportRequest(args);

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
    builder.Services.AddTransient<IConsoleService, ConsoleService>();
    builder.Services.AddSingleton<Application>();

    using IHost host = builder.Build();

    var app = host.Services.GetRequiredService<Application>();
    app.Run(filePath, request);
}
catch (Exception ex)
{
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"An error occurred: {ex.Message}");
    Console.ForegroundColor = originalColor;
    Environment.Exit(1);
}

static void PrintUsage()
{
    Console.WriteLine("Usage: ElectricGenerationParser <path-to-csv> [options]");
    Console.WriteLine();
    Console.WriteLine("Options (all optional):");
    Console.WriteLine("  --from <yyyy-MM-dd>   Start date of the window (from the cutoff hour on this day).");
    Console.WriteLine("  --to <yyyy-MM-dd>     End date of the window (up to the cutoff hour). Must be paired with --from.");
    Console.WriteLine("  --cutoff <0-24>       Hour that splits each day (default 12). 0=start-of-day,");
    Console.WriteLine("                        24=end-of-day (excludes From date, includes To date).");
    Console.WriteLine("  --plan <7|8|9|10>     Time-Of-Use plan start hour: 7=7am-7pm, 8=8am-8pm,");
    Console.WriteLine("                        9=9am-9pm, 10=10am-10pm (default 7).");
    Console.WriteLine();
    Console.WriteLine("When --from/--to are omitted, the entire file is summarized.");
}

static ReportRequest ParseReportRequest(string[] args)
{
    var request = new ReportRequest();

    // args[0] is the file path; scan the rest for --option value pairs.
    for (int i = 1; i < args.Length; i++)
    {
        string option = args[i];
        string? Value()
        {
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for option '{option}'.");
            }
            return args[++i];
        }

        switch (option)
        {
            case "--from":
                request.FromDate = ParseDate(Value()!, option);
                break;
            case "--to":
                request.ToDate = ParseDate(Value()!, option);
                break;
            case "--cutoff":
                request.CutoffHour = ParseCutoff(Value()!);
                break;
            case "--plan":
                request.Plan = ParsePlan(Value()!);
                break;
            default:
                throw new ArgumentException($"Unknown option '{option}'. Use --help to see usage.");
        }
    }

    if (request.FromDate.HasValue != request.ToDate.HasValue)
    {
        throw new ArgumentException("--from and --to must be provided together.");
    }

    return request;
}

static DateOnly ParseDate(string value, string option) =>
    DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None, out var date)
        ? date
        : throw new ArgumentException($"Invalid date '{value}' for option '{option}'. Expected yyyy-MM-dd.");

static int ParseCutoff(string value) =>
    int.TryParse(value, out var hour) && hour is >= 0 and <= 24
        ? hour
        : throw new ArgumentException($"Invalid --cutoff '{value}'. Expected an integer 0-24.");

static TimeOfUsePlan ParsePlan(string value) => value switch
{
    "7" => TimeOfUsePlan.SevenToSeven,
    "8" => TimeOfUsePlan.EightToEight,
    "9" => TimeOfUsePlan.NineToNine,
    "10" => TimeOfUsePlan.TenToTen,
    _ => throw new ArgumentException($"Invalid --plan '{value}'. Expected 7, 8, 9, or 10.")
};
