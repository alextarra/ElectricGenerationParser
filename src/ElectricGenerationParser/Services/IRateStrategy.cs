using ElectricGenerationParser.Models;

namespace ElectricGenerationParser.Services;

public interface IRateStrategy
{
    RateType? DetermineRate(DateTime timestamp);
}
