using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Services;

public interface IConsoleService
{
    void RenderReport(ReportModel model);
}
