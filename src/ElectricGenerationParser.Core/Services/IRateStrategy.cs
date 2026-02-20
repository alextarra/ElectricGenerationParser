using ElectricGenerationParser.Core.Models;

namespace ElectricGenerationParser.Core.Services;

public interface IRateStrategy
{
    RateType? DetermineRate(DateTime timestamp);
}
