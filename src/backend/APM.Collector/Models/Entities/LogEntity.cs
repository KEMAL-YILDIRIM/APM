using Azure;
using Azure.Data.Tables;

namespace APM.Collector.Models.Entities;

/// <summary>
/// Azure Table Storage entity for log records.
/// PartitionKey: {ApplicationId}_{YYYY-MM-DD}
/// RowKey: {InvertedTicks}_{GUID}
/// </summary>
public class LogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Log data
    public DateTimeOffset LogTimestamp { get; set; }
    public int SeverityNumber { get; set; }
    public string SeverityText { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Trace correlation
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }

    // Attributes (stored as JSON)
    public string? ResourceAttributes { get; set; }
    public string? LogAttributes { get; set; }

    // Application info
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? Environment { get; set; }

    /// <summary>
    /// Creates partition key from application ID and date.
    /// </summary>
    public static string CreatePartitionKey(string applicationId, DateTimeOffset timestamp)
    {
        return $"{applicationId}_{timestamp:yyyy-MM-dd}";
    }

    /// <summary>
    /// Creates row key with inverted timestamp for reverse chronological ordering.
    /// </summary>
    public static string CreateRowKey(DateTimeOffset timestamp)
    {
        var invertedTicks = DateTimeOffset.MaxValue.Ticks - timestamp.Ticks;
        return $"{invertedTicks:D19}_{Guid.NewGuid():N}";
    }
}
