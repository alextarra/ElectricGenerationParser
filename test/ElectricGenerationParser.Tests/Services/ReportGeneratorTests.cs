using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;
using ElectricGenerationParser.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace ElectricGenerationParser.Tests.Services;

public class ReportGeneratorTests
{
    private static ReportRequest DefaultRequest() => new();

    [Fact]
    public void GenerateReport_ShouldSumCorrectly_ForSingleRate()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OnPeak);

        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());
        var records = new List<GenerationRecord>
        {
            new() { Timestamp = new DateTime(2026, 1, 5, 10, 0, 0), Produced = 100, Consumed = 50 }, // Export=50, Import=0
            new() { Timestamp = new DateTime(2026, 1, 5, 11, 0, 0), Produced = 20, Consumed = 80 }  // Export=0, Import=60
        };

        // Act
        var report = generator.GenerateReport(records, DefaultRequest());

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
        Assert.Equal(10, report.GrandTotal.NetImport); // Import(60) - Export(50)
    }

    [Fact]
    public void GenerateReport_ShouldSplitByRateType()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        // 1st call -> OnPeak, 2nd call -> OffPeak
        calculatorMock.SetupSequence(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>()))
            .Returns(RateType.OnPeak)
            .Returns(RateType.OffPeak);

        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());
        var records = new List<GenerationRecord>
        {
            new() { Produced = 100, Consumed = 0 }, // OnPeak, Export=100
            new() { Produced = 0, Consumed = 50 }   // OffPeak, Import=50
        };

        // Act
        var report = generator.GenerateReport(records, DefaultRequest());

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
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OnPeak);

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
        var report = generator.GenerateReport(records, DefaultRequest());

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
        Assert.Throws<ElectricGenerationParser.Core.Exceptions.ValidationException>(() => generator.ValidateChecksums(invalidReport));
    }

    [Fact]
    public void GenerateReport_ShouldAggregateHolidays()
    {
        // Arrange
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OffPeak);

        var holidayServiceMock = new Mock<IHolidayService>();
        var holidayDate = new DateOnly(2026, 12, 25);
        holidayServiceMock.Setup(x => x.GetHolidayName(holidayDate)).Returns("Christmas");

        var generator = new ReportGenerator(calculatorMock.Object, holidayServiceMock.Object);
        var records = new List<GenerationRecord>
        {
            new() { Timestamp = holidayDate.ToDateTime(new TimeOnly(12, 0)), Produced = 100 }
        };

        // Act
        var report = generator.GenerateReport(records, DefaultRequest());

        // Assert
        Assert.True(report.HolidaySummaries.ContainsKey(holidayDate));
        Assert.Equal("Christmas", report.HolidaySummaries[holidayDate].Name);
        Assert.Equal(100, report.HolidaySummaries[holidayDate].Produced);
    }

    [Fact]
    public void GenerateReport_ShouldApplyMidDayToMidDayWindow()
    {
        // Arrange: cutoff hour 12 (noon). Window [7/2 12:00, 7/4 12:00).
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OnPeak);
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());

        var records = new List<GenerationRecord>
        {
            new() { Timestamp = new DateTime(2026, 7, 2, 11, 0, 0), Produced = 1 },  // before window start -> excluded
            new() { Timestamp = new DateTime(2026, 7, 2, 12, 0, 0), Produced = 10 }, // window start inclusive -> included
            new() { Timestamp = new DateTime(2026, 7, 3, 0, 0, 0), Produced = 100 }, // middle -> included
            new() { Timestamp = new DateTime(2026, 7, 4, 11, 0, 0), Produced = 1000 }, // before window end -> included
            new() { Timestamp = new DateTime(2026, 7, 4, 12, 0, 0), Produced = 1 },  // window end exclusive -> excluded
        };

        var request = new ReportRequest
        {
            FromDate = new DateOnly(2026, 7, 2),
            ToDate = new DateOnly(2026, 7, 4),
            CutoffHour = 12,
            Plan = TimeOfUsePlan.SevenToSeven
        };

        // Act
        var report = generator.GenerateReport(records, request);

        // Assert: only the three in-window records (10 + 100 + 1000) counted.
        Assert.Equal(1110, report.GrandTotal.Produced);
    }

    // Shared scenario for the cutoff-sensitivity tests below. A plain working week
    // (Mon 7/6 -> Fri 7/10 2026, all weekdays, no holidays) isolates the cutoff behavior:
    // only the weekday boundary hours move in/out of the window as the cutoff changes.
    //   cutoff 6  -> [7/6 06:00, 7/10 06:00): includes Mon 7/6 daytime, excludes Fri 7/10 daytime
    //   cutoff 18 -> [7/6 18:00, 7/10 18:00): excludes Mon 7/6 daytime, includes Fri 7/10 daytime
    private static (ReportModel low, ReportModel high) RunWorkingWeekWithTwoCutoffs()
    {
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>()))
            .Returns((DateTime ts, PeakPeriod _) => ts.Hour is >= 7 and < 19 ? RateType.OnPeak : RateType.OffPeak);

        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());

        List<GenerationRecord> Records() => new()
        {
            new() { Timestamp = new DateTime(2026, 7, 6, 7, 0, 0), Produced = 100, Consumed = 20 },    // Mon daytime: low only
            new() { Timestamp = new DateTime(2026, 7, 10, 10, 0, 0), Produced = 200, Consumed = 250 }, // Fri daytime: high only
            new() { Timestamp = new DateTime(2026, 7, 8, 12, 0, 0), Produced = 50, Consumed = 10 },    // Wed daytime: both (interior)
        };

        ReportRequest Request(int cutoff) => new()
        {
            FromDate = new DateOnly(2026, 7, 6),
            ToDate = new DateOnly(2026, 7, 10),
            CutoffHour = cutoff,
            Plan = TimeOfUsePlan.SevenToSeven
        };

        return (generator.GenerateReport(Records(), Request(6)),
                generator.GenerateReport(Records(), Request(18)));
    }

    [Fact]
    public void GenerateReport_CutoffHour_ShiftsGrandTotal()
    {
        var (low, high) = RunWorkingWeekWithTwoCutoffs();

        // The weekday boundary hours moved in/out, so totals differ.
        Assert.Equal(150, low.GrandTotal.Produced);  // 100 (Mon) + 50 (Wed)
        Assert.Equal(250, high.GrandTotal.Produced); // 200 (Fri) + 50 (Wed)
        Assert.NotEqual(low.GrandTotal.Consumed, high.GrandTotal.Consumed); // 30 vs 260
    }

    [Fact]
    public void GenerateReport_CutoffHour_ShiftsOnPeakSplit()
    {
        var (low, high) = RunWorkingWeekWithTwoCutoffs();

        Assert.Equal(150, low.Summaries[RateType.OnPeak].Produced);
        Assert.Equal(250, high.Summaries[RateType.OnPeak].Produced);
    }

    // Scenario for the invariance tests below: window Thu 7/2 -> Wed 7/8 2026 with WEEKDAY
    // edges, and the weekend + holiday hours placed strictly interior so they stay inside
    // BOTH cutoff windows. Weekend and holiday are classified per calendar date (before the
    // cutoff window is applied), so these summaries must not change when the cutoff moves.
    // July 4 2026 is a Saturday -> Independence Day observed Fri 7/3.
    private static (ReportModel low, ReportModel high) RunWeekWithInteriorWeekendAndHoliday()
    {
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>()))
            .Returns((DateTime ts, PeakPeriod _) => ts.Hour is >= 7 and < 19 ? RateType.OnPeak : RateType.OffPeak);

        // Real HolidayService so the observed-date logic (Sat -> preceding Fri) is genuine.
        var holidayService = new HolidayService(Options.Create(new HolidaySettings
        {
            FixedHolidays = new List<FixedHoliday> { new() { Name = "Independence Day", Month = 7, Day = 4 } },
            ObserveWeekendHolidays = true
        }));

        var generator = new ReportGenerator(calculatorMock.Object, holidayService);

        List<GenerationRecord> Records() => new()
        {
            // Weekday, non-holiday boundary hours that shift in/out with the cutoff:
            new() { Timestamp = new DateTime(2026, 7, 2, 7, 0, 0), Produced = 100, Consumed = 20 },   // Thu daytime: low only
            new() { Timestamp = new DateTime(2026, 7, 8, 10, 0, 0), Produced = 200, Consumed = 250 }, // Wed daytime: high only
            // Interior holiday + weekend hours, inside both windows regardless of cutoff:
            new() { Timestamp = new DateTime(2026, 7, 3, 12, 0, 0), Produced = 50, Consumed = 10 },   // Fri: Independence Day (Observed)
            new() { Timestamp = new DateTime(2026, 7, 4, 12, 0, 0), Produced = 40, Consumed = 8 },    // Sat: Independence Day + weekend
            new() { Timestamp = new DateTime(2026, 7, 5, 12, 0, 0), Produced = 30, Consumed = 5 },    // Sun: weekend
        };

        ReportRequest Request(int cutoff) => new()
        {
            FromDate = new DateOnly(2026, 7, 2),
            ToDate = new DateOnly(2026, 7, 8),
            CutoffHour = cutoff,
            Plan = TimeOfUsePlan.SevenToSeven
        };

        return (generator.GenerateReport(Records(), Request(6)),
                generator.GenerateReport(Records(), Request(18)));
    }

    [Fact]
    public void GenerateReport_CutoffHour_DoesNotChangeWeekendTotal()
    {
        var (low, high) = RunWeekWithInteriorWeekendAndHoliday();

        Assert.Equal(70, low.WeekendTotal.Produced); // Sat 40 + Sun 30
        Assert.Equal(low.WeekendTotal.Produced, high.WeekendTotal.Produced);
        Assert.Equal(low.WeekendTotal.Consumed, high.WeekendTotal.Consumed);
        Assert.Equal(low.WeekendTotal.Export, high.WeekendTotal.Export);
        Assert.Equal(low.WeekendTotal.Import, high.WeekendTotal.Import);
    }

    [Fact]
    public void GenerateReport_CutoffHour_DoesNotChangeHolidaySummary()
    {
        var (low, high) = RunWeekWithInteriorWeekendAndHoliday();

        // Same holiday dates (observed Fri 7/3 + actual Sat 7/4) with identical values.
        Assert.Equal(low.HolidaySummaries.Keys.OrderBy(d => d),
                     high.HolidaySummaries.Keys.OrderBy(d => d));
        Assert.Equal(new[] { new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 4) },
                     low.HolidaySummaries.Keys.OrderBy(d => d));
        Assert.Equal(low.HolidaySummaries[new DateOnly(2026, 7, 3)].Produced,
                     high.HolidaySummaries[new DateOnly(2026, 7, 3)].Produced);
        Assert.Equal(low.HolidaySummaries[new DateOnly(2026, 7, 4)].Produced,
                     high.HolidaySummaries[new DateOnly(2026, 7, 4)].Produced);
    }

    [Fact]
    public void GenerateReport_ShouldHandleFifteenMinuteReadings()
    {
        // Arrange: four 15-minute readings within a single hour, all summed.
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OnPeak);
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());

        var records = new List<GenerationRecord>
        {
            new() { Timestamp = new DateTime(2026, 7, 2, 13, 0, 0), Produced = 25, Consumed = 5 },
            new() { Timestamp = new DateTime(2026, 7, 2, 13, 15, 0), Produced = 25, Consumed = 5 },
            new() { Timestamp = new DateTime(2026, 7, 2, 13, 30, 0), Produced = 25, Consumed = 5 },
            new() { Timestamp = new DateTime(2026, 7, 2, 13, 45, 0), Produced = 25, Consumed = 5 },
        };

        // Act
        var report = generator.GenerateReport(records, DefaultRequest());

        // Assert
        Assert.Equal(100, report.GrandTotal.Produced);
        Assert.Equal(20, report.GrandTotal.Consumed);
        Assert.Equal(80, report.GrandTotal.Export); // net (100-20) exported
    }

    [Fact]
    public void GenerateReport_Cutoff0_vs_Cutoff24_FlipsWhichEndpointDateIsIncluded()
    {
        // The window is always [From @ cutoff, To @ cutoff). At the whole-day extremes:
        //   cutoff 0  -> [7/6 00:00, 7/8 00:00): includes the From date (7/6), excludes the To date (7/8).
        //   cutoff 24 -> [7/7 00:00, 7/9 00:00): excludes the From date (7/6), includes the To date (7/8).
        var calculatorMock = new Mock<IRateCalculator>();
        calculatorMock.Setup(x => x.CalculateRate(It.IsAny<DateTime>(), It.IsAny<PeakPeriod>())).Returns(RateType.OnPeak);
        var generator = new ReportGenerator(calculatorMock.Object, Mock.Of<IHolidayService>());

        List<GenerationRecord> Records() => new()
        {
            new() { Timestamp = new DateTime(2026, 7, 6, 12, 0, 0), Produced = 100 }, // From date
            new() { Timestamp = new DateTime(2026, 7, 7, 12, 0, 0), Produced = 50 },  // middle
            new() { Timestamp = new DateTime(2026, 7, 8, 12, 0, 0), Produced = 200 }, // To date
        };
        ReportRequest Request(int cutoff) => new()
        {
            FromDate = new DateOnly(2026, 7, 6),
            ToDate = new DateOnly(2026, 7, 8),
            CutoffHour = cutoff,
            Plan = TimeOfUsePlan.SevenToSeven
        };

        // cutoff 0: From date included, To date excluded.
        Assert.Equal(150, generator.GenerateReport(Records(), Request(0)).GrandTotal.Produced);  // 100 (7/6) + 50 (7/7)
        // cutoff 24: From date excluded, To date included.
        Assert.Equal(250, generator.GenerateReport(Records(), Request(24)).GrandTotal.Produced); // 50 (7/7) + 200 (7/8)
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    public void GenerateReport_ShouldThrow_ForInvalidCutoffHour(int cutoff)
    {
        var generator = new ReportGenerator(Mock.Of<IRateCalculator>(), Mock.Of<IHolidayService>());
        var request = new ReportRequest { CutoffHour = cutoff };

        Assert.Throws<ElectricGenerationParser.Core.Exceptions.ValidationException>(
            () => generator.GenerateReport(new List<GenerationRecord>(), request));
    }

    [Fact]
    public void GenerateReport_ShouldThrow_WhenFromDateAfterToDate()
    {
        var generator = new ReportGenerator(Mock.Of<IRateCalculator>(), Mock.Of<IHolidayService>());
        var request = new ReportRequest
        {
            FromDate = new DateOnly(2026, 7, 10),
            ToDate = new DateOnly(2026, 7, 1)
        };

        Assert.Throws<ElectricGenerationParser.Core.Exceptions.ValidationException>(
            () => generator.GenerateReport(new List<GenerationRecord>(), request));
    }
}
