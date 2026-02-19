namespace ElectricGenerationParser.Models;

public class GenerationRecord
{
    public DateTime Timestamp { get; set; }
    public decimal Produced { get; set; }
    public decimal Consumed { get; set; }
    public decimal Export { get; set; }
    public decimal Import { get; set; }
}
