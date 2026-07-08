using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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
    private const string CsvBytesSession = "egp_csvBytes";
    private const string CsvNameSession = "egp_csvName";

    private static IFormFile CreateFormFile(string fileName)
    {
        var stream = new MemoryStream("a,b,c\n1,2,3\n"u8.ToArray());
        return new FormFile(stream, 0, stream.Length, "Upload", fileName);
    }

    /// <summary>
    /// Minimal in-memory ISession so page-model session reads/writes work in unit tests.
    /// </summary>
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Builds an IndexModel wired to a real (default) HttpContext + fake session so cookie and
    /// session reads/writes work, with all dependencies mocked.
    /// </summary>
    private static (IndexModel model, DefaultHttpContext http) CreateModel(
        IFormFile? upload = null,
        Mock<ICsvParserService>? parser = null,
        Mock<IReportGenerator>? generator = null)
    {
        var logger = new Mock<ILogger<IndexModel>>();
        parser ??= new Mock<ICsvParserService>();
        generator ??= new Mock<IReportGenerator>();

        var http = new DefaultHttpContext { Session = new TestSession() };
        var model = new IndexModel(logger.Object, parser.Object, generator.Object)
        {
            Upload = upload,
            PageContext = new PageContext { HttpContext = http }
        };
        return (model, http);
    }

    [Fact]
    public void OnPost_WhenUploadMissingAndNoCache_AddsModelErrorAndReturnsPage()
    {
        var (model, _) = CreateModel(upload: null);

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public void OnPost_WhenUploadExtensionNotCsv_AddsModelErrorAndReturnsPage()
    {
        var (model, _) = CreateModel(upload: CreateFormFile("report.txt"));

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public void OnPost_WhenParserThrowsInvalidDataException_ShowsExceptionMessage()
    {
        var parser = new Mock<ICsvParserService>();
        parser
            .Setup(p => p.Parse(It.IsAny<Stream>()))
            .Throws(new InvalidDataException("bad csv"));

        var (model, _) = CreateModel(upload: CreateFormFile("report.csv"), parser: parser);

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("bad csv")));
    }

    [Fact]
    public void OnPost_WhenGeneratorThrowsValidationException_ShowsExceptionMessage()
    {
        var parser = new Mock<ICsvParserService>();
        parser
            .Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns(new List<GenerationRecord>());

        var generator = new Mock<IReportGenerator>();
        generator
            .Setup(g => g.GenerateReport(It.IsAny<List<GenerationRecord>>(), It.IsAny<ReportRequest>()))
            .Throws(new ValidationException("checksum mismatch"));

        var (model, _) = CreateModel(upload: CreateFormFile("report.csv"), parser: parser, generator: generator);

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage.Contains("checksum mismatch")));
    }

    [Fact]
    public void OnPost_WhenSuccessful_SetsReportAndReturnsPage()
    {
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

        var parser = new Mock<ICsvParserService>();
        parser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(records);
        var generator = new Mock<IReportGenerator>();
        generator.Setup(g => g.GenerateReport(records, It.IsAny<ReportRequest>())).Returns(report);

        var (model, _) = CreateModel(upload: CreateFormFile("report.csv"), parser: parser, generator: generator);

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Report);
        Assert.Equal(1, model.Report!.GrandTotal.Produced);
    }

    [Fact]
    public void OnPost_WhenSettingsValid_WritesPersistentCutoffAndPlanCookies()
    {
        var (model, http) = CreateModel(upload: null);
        model.CutoffHour = 9;
        model.Plan = TimeOfUsePlan.NineToNine;

        model.OnPost(); // upload is null, but cookies are still saved from valid settings

        var setCookies = http.Response.Headers.SetCookie.ToString();
        Assert.Contains("egp_cutoffHour=9", setCookies);
        Assert.Contains($"egp_plan={TimeOfUsePlan.NineToNine}", setCookies);
        // Persistent cookies carry an expiry.
        Assert.Contains("egp_cutoffHour=9; expires=", setCookies);
    }

    [Fact]
    public void OnPost_WritesSessionDateCookies_AndCachesUploadedFile()
    {
        var (model, http) = CreateModel(upload: CreateFormFile("report.csv"));
        model.FromDate = new DateOnly(2026, 1, 5);
        model.ToDate = new DateOnly(2026, 1, 10);

        model.OnPost();

        var setCookies = http.Response.Headers.SetCookie.ToString();
        Assert.Contains("egp_fromDate=2026-01-05", setCookies);
        Assert.Contains("egp_toDate=2026-01-10", setCookies);
        // Session cookies have no expiry (dropped when the browser closes).
        Assert.DoesNotContain("egp_fromDate=2026-01-05; expires=", setCookies);

        // The uploaded CSV is cached server-side in session.
        Assert.Equal("report.csv", http.Session.GetString(CsvNameSession));
        Assert.NotNull(http.Session.Get(CsvBytesSession));
        Assert.Equal("report.csv", model.LastFileName);
    }

    [Fact]
    public void OnPost_WithoutDates_ClearsRememberedDateCookies()
    {
        var (model, http) = CreateModel(upload: CreateFormFile("report.csv"));
        model.FromDate = null;
        model.ToDate = null;

        model.OnPost();

        var setCookies = http.Response.Headers.SetCookie.ToString();
        // Deletion emits an already-expired cookie.
        Assert.Contains("egp_fromDate=; expires=Thu, 01 Jan 1970", setCookies);
        Assert.Contains("egp_toDate=; expires=Thu, 01 Jan 1970", setCookies);
    }

    [Fact]
    public void OnPost_WhenNoUploadButSessionHasCachedCsv_UsesCachedFile()
    {
        var records = new List<GenerationRecord> { new() { Produced = 5 } };
        var report = new ReportModel { GrandTotal = new MetricSummary { Produced = 5 } };

        var parser = new Mock<ICsvParserService>();
        parser.Setup(p => p.Parse(It.IsAny<Stream>())).Returns(records);
        var generator = new Mock<IReportGenerator>();
        generator.Setup(g => g.GenerateReport(records, It.IsAny<ReportRequest>())).Returns(report);

        var (model, http) = CreateModel(upload: null, parser: parser, generator: generator);
        // Pre-seed the session as if a file was uploaded earlier this session.
        http.Session.Set(CsvBytesSession, "a,b,c\n1,2,3\n"u8.ToArray());
        http.Session.SetString(CsvNameSession, "cached.csv");

        var result = model.OnPost();

        Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.IsValid);
        Assert.NotNull(model.Report);
        Assert.Equal(5, model.Report!.GrandTotal.Produced);
        Assert.Equal("cached.csv", model.LastFileName);
        parser.Verify(p => p.Parse(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public void OnGet_SeedsDatesFromCookies_AndFileNameFromSession()
    {
        var (model, http) = CreateModel();
        http.Request.Headers.Cookie = "egp_fromDate=2026-02-01; egp_toDate=2026-02-05";
        http.Session.SetString(CsvNameSession, "data.csv");

        model.OnGet();

        Assert.Equal(new DateOnly(2026, 2, 1), model.FromDate);
        Assert.Equal(new DateOnly(2026, 2, 5), model.ToDate);
        Assert.Equal("data.csv", model.LastFileName);
    }

    [Fact]
    public void OnGet_WhenCookiesPresent_SeedsCutoffAndPlanDefaults()
    {
        var (model, http) = CreateModel();
        http.Request.Headers.Cookie = $"egp_cutoffHour=8; egp_plan={TimeOfUsePlan.TenToTen}";

        model.OnGet();

        Assert.Equal(8, model.CutoffHour);
        Assert.Equal(TimeOfUsePlan.TenToTen, model.Plan);
    }

    [Fact]
    public void OnGet_WhenNoCookies_KeepsDefaults()
    {
        var (model, _) = CreateModel();

        model.OnGet();

        Assert.Equal(12, model.CutoffHour);
        Assert.Equal(TimeOfUsePlan.SevenToSeven, model.Plan);
    }
}
