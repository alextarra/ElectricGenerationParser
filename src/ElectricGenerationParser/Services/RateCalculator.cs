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
        
        // Default to OffPeak if no strategy matches (safe fallback), or throw?
        // Requirement says "Weekday outside Peak Hours = OffPeak".
        // WeekdayStrategy should handle the "rest".
        // Let's assume strategies are exhaustive. If not, OffPeak is a safer default for consumers than OnPeak.
        return RateType.OffPeak;
    }
}
