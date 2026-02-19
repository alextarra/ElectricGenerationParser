using CsvHelper.Configuration;
using ElectricGenerationParser.Models;
using System.Globalization;

namespace ElectricGenerationParser.Services;

public sealed class GenerationRecordMap : ClassMap<GenerationRecord>
{
    public GenerationRecordMap()
    {
        Map(m => m.Timestamp).Convert(args => 
        {
            var dateStr = args.Row.GetField("Date");
            var timeStr = args.Row.GetField("Time");
            
            // "01/01/2025" and "00:00"
            if (DateOnly.TryParseExact(dateStr, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                TimeOnly.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                return date.ToDateTime(time);
            }
            throw new InvalidDataException($"Invalid Date/Time format: '{dateStr}' '{timeStr}' at row {args.Row.Parser.Row}");
        });

        Map(m => m.Produced).Name("Energy Produced (Wh)");
        Map(m => m.Consumed).Name("Energy Consumed (Wh)");
        
        // Explicitly ignore properties not in CSV to allow strict validation elsewhere
        Map(m => m.Export).Ignore();
        Map(m => m.Import).Ignore();
    }
}
