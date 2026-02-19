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
            var dateTimeStr = args.Row.GetField("Date/Time");
            
            if (DateTime.TryParseExact(dateTimeStr, "MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return dateTime;
            }
            // Try parse without exact format as fallback if needed
            if (DateTime.TryParse(dateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }

            throw new InvalidDataException($"Invalid Date/Time format: '{dateTimeStr}' at row {args.Row.Parser.Row}");
        });

        Map(m => m.Produced).Name("Energy Produced (Wh)");
        Map(m => m.Consumed).Name("Energy Consumed (Wh)");
        Map(m => m.Export).Name("Exported to Grid (Wh)");
        Map(m => m.Import).Name("Imported from Grid (Wh)");
    }
}
