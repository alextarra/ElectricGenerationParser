using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion; // Added for TypeConversionException
using ElectricGenerationParser.Models;
using System.Globalization;

namespace ElectricGenerationParser.Services;

public interface ICsvParserService
{
    List<GenerationRecord> Parse(string filePath);
}

public class CsvParserService : ICsvParserService
{
    public List<GenerationRecord> Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file not found.", filePath);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        };
        
        // Validation functions (default behavior is good, but explicit is better for clarity)
        config.HeaderValidated = ConfigurationFunctions.HeaderValidated;
        config.MissingFieldFound = ConfigurationFunctions.MissingFieldFound;

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<GenerationRecordMap>();

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
            throw new InvalidDataException($"Data Format Error at Row {ex.Context.Parser.Row}: {ex.Message}", ex);
        }
        // Catch other CsvHelper exceptions generally
        catch (CsvHelperException ex)
        {
             throw new InvalidDataException($"CSV Parsing Error at Row {ex.Context.Parser.Row}: {ex.Message}", ex);
        }
    }
}
