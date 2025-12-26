namespace Racelogic.APM;

/// <summary>
/// Configuration options for the APM SDK.
/// </summary>
public class ApmOptions
{
    /// <summary>
    /// The APM Collector endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:5000";

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The unique identifier for this application.
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// The human-readable name for this application.
    /// </summary>
    public string ApplicationName { get; set; } = "Unknown";

    /// <summary>
    /// The environment (e.g., development, staging, production).
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// The service version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Number of records to batch before sending.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Interval in milliseconds to flush the buffer.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable automatic instrumentation of common libraries.
    /// </summary>
    public bool EnableAutoInstrumentation { get; set; } = true;
}
