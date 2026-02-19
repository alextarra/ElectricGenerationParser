using ElectricGenerationParser.Models;

namespace ElectricGenerationParser.Services;

public interface IRateCalculator
{
    RateType CalculateRate(DateTime timestamp);
}

public class RateCalculator : IRateCalculator
{
    private readonly IEnumerable<IRateStrategy> _strategies;

    public RateCalculator(IEnumerable<IRateStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    public RateType CalculateRate(DateTime timestamp)
    {
        foreach (var strategy in _strategies)
        {
            var rate = strategy.DetermineRate(timestamp);
            if (rate.HasValue)
            {
                return rate.Value;
            }
        }

        throw new InvalidOperationException(
            $"No rate strategy resolved rate for timestamp '{timestamp:O}'. Verify strategy registration and configuration.");
    }
}
