using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricGenerationParser.Core.Services;
using ElectricGenerationParser.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace ElectricGenerationParser.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ICsvParserService _csvParserService;
    private readonly IReportGenerator _reportGenerator;

    [BindProperty]
    public IFormFile? Upload { get; set; }

    [BindProperty]
    [Display(Name = "Date From")]
    public DateOnly? FromDate { get; set; }

    [BindProperty]
    [Display(Name = "Date To")]
    public DateOnly? ToDate { get; set; }

    [BindProperty]
    [Display(Name = "Cutoff Hour")]
    [Range(0, 24, ErrorMessage = "Cutoff Hour must be between 0 and 24.")]
    public int CutoffHour { get; set; } = 12;

    [BindProperty]
    [Display(Name = "Time-Of-Use Plan")]
    public TimeOfUsePlan Plan { get; set; } = TimeOfUsePlan.SevenToSeven;

    public ReportModel? Report { get; set; }

    /// <summary>
    /// Name of the CSV cached for this browser session. When present, the report can be
    /// re-run with an empty file input and the cached file is used automatically.
    /// </summary>
    public string? LastFileName { get; set; }

    // Persistent cookies: last-used settings remembered across browser sessions.
    private const string CutoffHourCookie = "egp_cutoffHour";
    private const string PlanCookie = "egp_plan";

    // Session cookies: remembered between "Process Report" clicks, cleared when the browser closes.
    private const string FromDateCookie = "egp_fromDate";
    private const string ToDateCookie = "egp_toDate";

    // Server-side session storage for the uploaded CSV (also session-scoped).
    private const string CsvBytesSession = "egp_csvBytes";
    private const string CsvNameSession = "egp_csvName";

    private const string DateFormat = "yyyy-MM-dd";

    public IndexModel(
        ILogger<IndexModel> logger,
        ICsvParserService csvParserService,
        IReportGenerator reportGenerator)
    {
        _logger = logger;
        _csvParserService = csvParserService;
        _reportGenerator = reportGenerator;
    }

    public void OnGet()
    {
        // Persistent settings: remembered across browser sessions.
        if (int.TryParse(Request.Cookies[CutoffHourCookie], out var savedCutoff) && savedCutoff is >= 0 and <= 24)
        {
            CutoffHour = savedCutoff;
        }
        if (Enum.TryParse<TimeOfUsePlan>(Request.Cookies[PlanCookie], out var savedPlan) && Enum.IsDefined(savedPlan))
        {
            Plan = savedPlan;
        }

        // Session context: remembered between submissions, gone when the browser closes.
        if (DateOnly.TryParseExact(Request.Cookies[FromDateCookie], DateFormat, out var savedFrom))
        {
            FromDate = savedFrom;
        }
        if (DateOnly.TryParseExact(Request.Cookies[ToDateCookie], DateFormat, out var savedTo))
        {
            ToDate = savedTo;
        }
        LastFileName = HttpContext.Session.GetString(CsvNameSession);
    }

    public IActionResult OnPost()
    {
        // Remember the submitted cutoff/plan for next time, as long as they're valid,
        // so the defaults persist even if this particular upload fails.
        if (CutoffHour is >= 0 and <= 24 && Enum.IsDefined(Plan))
        {
            SavePersistentSettings();
        }

        // Remember the date range for the rest of this browser session.
        SaveSessionDates();

        // Resolve the CSV: use the newly uploaded file if present, otherwise fall back to
        // the file cached earlier in this browser session.
        byte[]? csvBytes;
        if (Upload is { Length: > 0 })
        {
            var extension = Path.GetExtension(Upload.FileName);
            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                LastFileName = HttpContext.Session.GetString(CsvNameSession);
                ModelState.AddModelError("", "Only .csv files are supported.");
                return Page();
            }

            using var ms = new MemoryStream();
            Upload.CopyTo(ms);
            csvBytes = ms.ToArray();

            // Cache the file for the session so it can be reused without re-selecting.
            HttpContext.Session.Set(CsvBytesSession, csvBytes);
            HttpContext.Session.SetString(CsvNameSession, Upload.FileName);
        }
        else
        {
            csvBytes = HttpContext.Session.Get(CsvBytesSession);
        }

        LastFileName = HttpContext.Session.GetString(CsvNameSession);

        if (csvBytes == null || csvBytes.Length == 0)
        {
            ModelState.AddModelError("", "Please select a valid CSV file.");
            return Page();
        }

        // Date range is optional, but if one bound is provided both must be, and From <= To.
        if (FromDate.HasValue != ToDate.HasValue)
        {
            ModelState.AddModelError("", "Please provide both Date From and Date To, or leave both empty.");
            return Page();
        }
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value > ToDate.Value)
        {
            ModelState.AddModelError("", "Date From must be on or before Date To.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new ReportRequest
        {
            FromDate = FromDate,
            ToDate = ToDate,
            CutoffHour = CutoffHour,
            Plan = Plan
        };

        try
        {
            using var stream = new MemoryStream(csvBytes);
            var records = _csvParserService.Parse(stream);
            Report = _reportGenerator.GenerateReport(records, request);

            // Log success but keeping UI clean
            _logger.LogInformation("Successfully processed report with {RecordCount} records.", records.Count);

            return Page();
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid CSV upload.");
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        catch (ElectricGenerationParser.Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while generating report.");
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while generating report.");
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file.");
            ModelState.AddModelError("", "Processing failed. Please verify the CSV format and try again.");
            return Page();
        }
    }

    private void SavePersistentSettings()
    {
        var options = PersistentOptions();
        Response.Cookies.Append(CutoffHourCookie, CutoffHour.ToString(), options);
        Response.Cookies.Append(PlanCookie, Plan.ToString(), options);
    }

    private void SaveSessionDates()
    {
        var options = SessionOptions();

        // Dates are stored when both are set; otherwise the remembered range is cleared.
        if (FromDate.HasValue && ToDate.HasValue)
        {
            Response.Cookies.Append(FromDateCookie, FromDate.Value.ToString(DateFormat), options);
            Response.Cookies.Append(ToDateCookie, ToDate.Value.ToString(DateFormat), options);
        }
        else
        {
            Response.Cookies.Delete(FromDateCookie);
            Response.Cookies.Delete(ToDateCookie);
        }
    }

    // Persistent: survives browser restart (1-year expiry).
    private static CookieOptions PersistentOptions() => new()
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        IsEssential = true, // functional preference, not tracking
        HttpOnly = true,
        SameSite = SameSiteMode.Lax
    };

    // Session: no expiry, so the browser drops it when the session ends.
    private static CookieOptions SessionOptions() => new()
    {
        IsEssential = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax
    };
}
