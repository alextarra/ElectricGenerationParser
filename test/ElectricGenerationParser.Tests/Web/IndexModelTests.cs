using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using ElectricGenerationParser.Core.Models;
using ElectricGenerationParser.Core.Services;
using ElectricGenerationParser.Web.Pages;

namespace ElectricGenerationParser.Tests.Web;

public class IndexModelTests
{
    private static IFormFile CreateFormFile(string fileName)
    {
        var stream = new MemoryStream("a,b,c\n1,2,3\n"u8.ToArray());
        return new FormFile(stream, 0, stream.Length, "Upload", fileName);
    }

    [Fact]
    public void OnPost_WhenUploadMissing_AddsModelErrorAndReturnsPage()
    {
        var logger = new Mock<ILogger<IndexModel>>();
        var parser = new Mock<ICsvParserService>();
        var generator = new Mock<IReportGenerator>();

        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = null
        };

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public void OnPost_WhenUploadExtensionNotCsv_AddsModelErrorAndReturnsPage()
    {
        var logger = new Mock<ILogger<IndexModel>>();
        var parser = new Mock<ICsvParserService>();
        var generator = new Mock<IReportGenerator>();

        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = CreateFormFile("report.txt")
        };

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public void OnPost_WhenParserThrowsInvalidDataException_ShowsExceptionMessage()
    {
        var logger = new Mock<ILogger<IndexModel>>();
        var parser = new Mock<ICsvParserService>();
        var generator = new Mock<IReportGenerator>();

        parser
            .Setup(p => p.Parse(It.IsAny<Stream>()))
            .Throws(new InvalidDataException("bad csv"));

        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = CreateFormFile("report.csv")
        };

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("bad csv")));
    }

    [Fact]
    public void OnPost_WhenGeneratorThrowsValidationException_ShowsExceptionMessage()
    {
        var logger = new Mock<ILogger<IndexModel>>();
        var parser = new Mock<ICsvParserService>();
        var generator = new Mock<IReportGenerator>();

        parser
            .Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns(new List<GenerationRecord>());

        generator
            .Setup(g => g.GenerateReport(It.IsAny<List<GenerationRecord>>()))
            .Throws(new ValidationException("checksum mismatch"));

        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = CreateFormFile("report.csv")
        };

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("checksum mismatch")));
    }

    [Fact]
    public void OnPost_WhenSuccessful_SetsReportAndReturnsPage()
    {
        var logger = new Mock<ILogger<IndexModel>>();
        var parser = new Mock<ICsvParserService>();
        var generator = new Mock<IReportGenerator>();

        var records = new List<GenerationRecord>
        {
            new()
            {
                Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                Produced = 1,
                Consumed = 2,
                Export = 3,
                Import = 4
            }
        };

        var report = new ReportModel
        {
            GrandTotal = new MetricSummary { Produced = 1, Consumed = 2, Export = 3, Import = 4 }
        };

        parser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(records);
        generator.Setup(g => g.GenerateReport(records)).Returns(report);

        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = CreateFormFile("report.csv")
        };

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Report);
        Assert.Equal(1, model.Report!.GrandTotal.Produced);
    }
}
