using Azure;
using Azure.Data.Tables;

namespace APM.Collector.Models.Entities;

/// <summary>
/// Azure Table Storage entity for metric records.
/// </summary>
public class MetricEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Metric data
    public DateTimeOffset MetricTimestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string? MetricDescription { get; set; }
    public string? MetricUnit { get; set; }
    public string MetricType { get; set; } = "Gauge"; // Gauge, Counter, Histogram

    // Values
    public double Value { get; set; }
    public double? ValueMin { get; set; }
    public double? ValueMax { get; set; }
    public double? ValueSum { get; set; }
    public long? ValueCount { get; set; }

    // Attributes (stored as JSON)
    public string? Attributes { get; set; }
    public string? ResourceAttributes { get; set; }

    // Application info
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;

    public static string CreatePartitionKey(string applicationId, DateTimeOffset timestamp)
    {
        return $"{applicationId}_{timestamp:yyyy-MM-dd}";
    }

    public static string CreateRowKey(DateTimeOffset timestamp, string metricName)
    {
        var invertedTicks = DateTimeOffset.MaxValue.Ticks - timestamp.Ticks;
        return $"{invertedTicks:D19}_{metricName}_{Guid.NewGuid():N}";
    }
}
