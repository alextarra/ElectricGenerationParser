using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;

namespace ElectricGenerationParser.Tests.Services;

public class CsvParserServiceTests : IDisposable
{
    private readonly string _tempFile;

    public CsvParserServiceTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void Parse_ShouldReturnRecords_WhenCsvIsValid()
    {
        // Arrange
        var csvContent = 
@"Date/Time,Energy Produced (Wh),Energy Consumed (Wh),Exported to Grid (Wh),Imported from Grid (Wh)
01/01/2026 00:15,100.5,50.2,10,5.5
01/01/2026 00:30,200,60,20,10";
        File.WriteAllText(_tempFile, csvContent);
        var parser = new CsvParserService();

        // Act
        var result = parser.Parse(_tempFile);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 15, 0), result[0].Timestamp);
        Assert.Equal(100.5m, result[0].Produced);
        Assert.Equal(50.2m, result[0].Consumed);
        Assert.Equal(10m, result[0].Export);
        Assert.Equal(5.5m, result[0].Import);
    }

    [Fact]
    public void Parse_ShouldThrowInvalidDataException_WhenHeaderIsMissing()
    {
        // Arrange
        // Missing 'Energy Produced (Wh)' and others
        var csvContent = 
@"Date/Time,Energy Consumed (Wh)
01/01/2026 00:15,50.2";
        File.WriteAllText(_tempFile, csvContent);
        var parser = new CsvParserService();

        // Act & Assert
        try 
        {
            parser.Parse(_tempFile);
        }
        catch (InvalidDataException ex)
        {
            Assert.Contains("Header", ex.Message);
            return;
        }
        Assert.Fail("Expected InvalidDataException");
    }

    [Fact]
    public void Parse_ShouldThrowInvalidDataException_WithRowNumber_WhenDataIsMalformed()
    {
        // Arrange
        // Row 2 has text in decimal field
        var csvContent = 
@"Date/Time,Energy Produced (Wh),Energy Consumed (Wh),Exported to Grid (Wh),Imported from Grid (Wh)
01/01/2026 00:15,100,50,0,0
01/01/2026 00:30,N/A,60,0,0";
        File.WriteAllText(_tempFile, csvContent);
        var parser = new CsvParserService();


        // Act & Assert
        try 
        {
            parser.Parse(_tempFile);
        }
        catch (InvalidDataException ex)
        {
            // Row number might be 3 (header is 1, data is 2, error is 3) or 2 depending on parser config.
            // CsvHelper counts header as record if not ignored? No, raw row number usually.
            Assert.Contains("Data Format Error", ex.Message);
            Assert.Contains("Row 3", ex.Message); // Header(1) + Row1(2) + Row2(3)
            return;
        }
        Assert.Fail("Expected InvalidDataException");
    }
}
