using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion; // Added for TypeConversionException
using ElectricGenerationParser.Core.Models;
using System.Globalization;

namespace ElectricGenerationParser.Core.Services;

public interface ICsvParserService
{
    List<GenerationRecord> Parse(string filePath);
    List<GenerationRecord> Parse(Stream stream);
}

public class CsvParserService : ICsvParserService
{
    public List<GenerationRecord> Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file not found.", filePath);
        }

        using var stream = File.OpenRead(filePath);
        return Parse(stream);
    }

    public List<GenerationRecord> Parse(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        };
        
        // Validation functions (default behavior is good, but explicit is better for clarity)
        config.HeaderValidated = ConfigurationFunctions.HeaderValidated;
        config.MissingFieldFound = ConfigurationFunctions.MissingFieldFound;

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap(new GenerationRecordMap());

        try
        {
            return csv.GetRecords<GenerationRecord>().ToList();
        }
        catch (CsvHelper.HeaderValidationException ex)
        {
            throw new InvalidDataException($"CSV Header Validation Error: {ex.Message}", ex);
        }
        catch (CsvHelper.TypeConversion.TypeConverterException ex)
        {
            throw new InvalidDataException($"Data Format Error at Row {GetRowNumber(ex)}: {ex.Message}", ex);
        }
        // Catch other CsvHelper exceptions generally
        catch (CsvHelperException ex)
        {
             throw new InvalidDataException($"CSV Parsing Error at Row {GetRowNumber(ex)}: {ex.Message}", ex);
        }
    }

    private static string GetRowNumber(CsvHelperException ex)
    {
        var row = ex.Context?.Parser?.Row;
        return row.HasValue
            ? row.Value.ToString(CultureInfo.InvariantCulture)
            : "Unknown";
    }
}
