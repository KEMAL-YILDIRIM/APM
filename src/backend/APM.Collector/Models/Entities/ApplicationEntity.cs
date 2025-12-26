using Azure;
using Azure.Data.Tables;

namespace APM.Collector.Models.Entities;

/// <summary>
/// Azure Table Storage entity for registered applications.
/// </summary>
public class ApplicationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "applications";
    public string RowKey { get; set; } = string.Empty; // ApplicationId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty; // Hashed API key
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public string? Environment { get; set; }
    public string? Tags { get; set; } // JSON
    public bool IsActive { get; set; } = true;
}
