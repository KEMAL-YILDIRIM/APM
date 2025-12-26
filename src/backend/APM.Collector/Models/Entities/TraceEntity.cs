using Azure;
using Azure.Data.Tables;

namespace APM.Collector.Models.Entities;

/// <summary>
/// Azure Table Storage entity for trace span records.
/// </summary>
public class TraceEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Trace identifiers
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }

    // Span data
    public string SpanName { get; set; } = string.Empty;
    public string SpanKind { get; set; } = "Internal"; // Client, Server, Internal, Producer, Consumer
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public long DurationMs { get; set; }

    // Status
    public string StatusCode { get; set; } = "Unset"; // Unset, Ok, Error
    public string? StatusMessage { get; set; }

    // Attributes (stored as JSON)
    public string? Attributes { get; set; }
    public string? Events { get; set; }
    public string? ResourceAttributes { get; set; }

    // Application info
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;

    public static string CreatePartitionKey(string applicationId, DateTimeOffset timestamp)
    {
        return $"{applicationId}_{timestamp:yyyy-MM-dd}";
    }

    public static string CreateRowKey(DateTimeOffset timestamp, string spanId)
    {
        var invertedTicks = DateTimeOffset.MaxValue.Ticks - timestamp.Ticks;
        return $"{invertedTicks:D19}_{spanId}";
    }
}
