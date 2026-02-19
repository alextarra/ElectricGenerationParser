using ElectricGenerationParser.Models;

namespace ElectricGenerationParser.Services;

public interface IConsoleService
{
    void RenderReport(ReportModel model);
}
