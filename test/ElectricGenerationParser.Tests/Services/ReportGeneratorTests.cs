using ElectricGenerationParser.Models;
using ElectricGenerationParser.Services;
using Moq;

namespace ElectricGenerationParser.Tests.Services;

public class ReportGeneratorTests
{
    private class MockRateCalculator : IRateCalculator
    {
        private readonly RateType _fixedRate;
        public MockRateCalculator(RateType rate) => _fixedRate = rate;
        public RateType CalculateRate(DateTime timestamp) => _fixedRate;
    }

    [Fact]
    public void GenerateReport_ShouldSumCorrectly_ForSingleRate()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>())).Returns(RateType.OnPeak);
        
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());
        var records = new List<GenerationRecord>
        {
            new() { Timestamp = DateTime.Now, Produced = 100, Consumed = 50 }, // Expert=50, Import=0
            new() { Timestamp = DateTime.Now, Produced = 20, Consumed = 80 }  // Export=0, Import=60
        };

        // Act
        var report = generator.GenerateReport(records);

        // Assert
        var onPeak = report.Summaries[RateType.OnPeak];
        Assert.Equal(120, onPeak.Produced); // 100 + 20
        Assert.Equal(130, onPeak.Consumed); // 50 + 80
        Assert.Equal(50, onPeak.Export);    // 50 + 0
        Assert.Equal(60, onPeak.Import);    // 0 + 60
        
        Assert.Equal(120, report.GrandTotal.Produced);
        Assert.Equal(130, report.GrandTotal.Consumed);
        Assert.Equal(50, report.GrandTotal.Export);
        Assert.Equal(60, report.GrandTotal.Import);
    }

    [Fact]
    public void GenerateReport_ShouldSplitByRateType()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        // 1st call -> OnPeak, 2nd call -> OffPeak
        calculatorMock.SetupSequence(x => x.CalculateRate(It.IsAny<DateTime>()))
            .Returns(RateType.OnPeak)
            .Returns(RateType.OffPeak);
            
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());
        var records = new List<GenerationRecord>
        {
            new() { Produced = 100, Consumed = 0 }, // OnPeak, Export=100
            new() { Produced = 0, Consumed = 50 }   // OffPeak, Import=50
        };

        // Act
        var report = generator.GenerateReport(records);

        // Assert
        Assert.Equal(100, report.Summaries[RateType.OnPeak].Produced);
        Assert.Equal(0, report.Summaries[RateType.OffPeak].Produced);
        Assert.Equal(50, report.Summaries[RateType.OffPeak].Consumed);
    }


    [Fact]
    public void GenerateReport_ShouldNotModifyInputRecords()
    {
        // Fix for side effects: ensure GenerateReport does not change input records
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>())).Returns(RateType.OnPeak);
        
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());
        var originalProduced = 100m;
        var originalConsumed = 50m;
        var record = new GenerationRecord 
        { 
            Produced = originalProduced, 
            Consumed = originalConsumed,
            Export = 0, // Should remain 0
            Import = 0  // Should remain 0
        };
        var records = new List<GenerationRecord> { record };

        // Act
        var report = generator.GenerateReport(records);

        // Assert
        Assert.Equal(originalProduced, record.Produced);
        Assert.Equal(originalConsumed, record.Consumed);
        // Verify side effect removed: Export/Import should NOT be modified on the object itself
        Assert.Equal(0, record.Export); 
        Assert.Equal(0, record.Import);

        // However, the report MUST contain the calculated values
        Assert.Equal(50, report.GrandTotal.Export); // 100 - 50 = 50 net export
        Assert.Equal(0, report.GrandTotal.Import);
    }

    [Fact]
    public void ValidateChecksums_ShouldThrowException_WhenDataIsCorrupt()
    {
        // Now testing the internal validation logic directly
        var calculatorMock = new Mock<IRateCalculator>();
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());

        // Construct an invalid report manually
        var invalidReport = new ReportModel();
        invalidReport.Summaries[RateType.OnPeak] = new MetricSummary { Produced = 10 };
        // GrandTotal says 20, but sum of parts is 10. Mismatch!
        invalidReport.GrandTotal = new MetricSummary { Produced = 20 };

        // Act & Assert
        Assert.Throws<ElectricGenerationParser.Exceptions.ValidationException>(() => generator.ValidateChecksums(invalidReport));
    }

    [Fact]
    public void GenerateReport_ShouldAggregateHolidays()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>())).Returns(RateType.OffPeak);

        var holidayServiceMock = new Mock<IHolidayService>();
        var holidayDate = new DateOnly(2026, 12, 25);
        holidayServiceMock.Setup(x => x.GetHolidayName(holidayDate)).Returns("Christmas");

        var generator = new ReportGenerator(calculatorMock.Object, holidayServiceMock.Object);
        var records = new List<GenerationRecord>
        {
            new() { Timestamp = holidayDate.ToDateTime(new TimeOnly(12, 0)), Produced = 100 }
        };

        // Act
        var report = generator.GenerateReport(records);

        // Assert
        Assert.True(report.HolidaySummaries.ContainsKey(holidayDate));
        Assert.Equal("Christmas", report.HolidaySummaries[holidayDate].Name);
        Assert.Equal(100, report.HolidaySummaries[holidayDate].Produced);
    }
}
