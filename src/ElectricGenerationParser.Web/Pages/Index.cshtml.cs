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

    public ReportModel? Report { get; set; }

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
    }

    public IActionResult OnPost()
    {
        if (Upload == null || Upload.Length == 0)
        {
            ModelState.AddModelError("", "Please select a valid CSV file.");
            return Page();
        }

        var extension = Path.GetExtension(Upload.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Only .csv files are supported.");
            return Page();
        }

        try
        {
            using var stream = Upload.OpenReadStream();
            var records = _csvParserService.Parse(stream);
            Report = _reportGenerator.GenerateReport(records);
            
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
}
