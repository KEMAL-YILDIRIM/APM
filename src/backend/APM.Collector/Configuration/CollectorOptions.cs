namespace APM.Collector.Configuration;

public class CollectorOptions
{
    public int BatchSize { get; set; } = 100;
    public int FlushIntervalMs { get; set; } = 5000;
    public int MaxQueueSize { get; set; } = 10000;
}
