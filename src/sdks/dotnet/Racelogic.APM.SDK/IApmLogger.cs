namespace Racelogic.APM;

/// <summary>
/// Interface for APM logging functionality.
/// </summary>
public interface IApmLogger
{
    /// <summary>
    /// Log a trace message.
    /// </summary>
    void LogTrace(string message, object? attributes = null);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    void LogDebug(string message, object? attributes = null);

    /// <summary>
    /// Log an informational message.
    /// </summary>
    void LogInfo(string message, object? attributes = null);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    void LogWarn(string message, object? attributes = null);

    /// <summary>
    /// Log an error message.
    /// </summary>
    void LogError(string message, Exception? exception = null, object? attributes = null);

    /// <summary>
    /// Log a fatal error message.
    /// </summary>
    void LogFatal(string message, Exception? exception = null, object? attributes = null);

    /// <summary>
    /// Flush all pending logs immediately.
    /// </summary>
    Task FlushAsync();
}
