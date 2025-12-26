namespace Racelogic.APM;

/// <summary>
/// Interface for APM metrics functionality.
/// </summary>
public interface IApmMetrics
{
    /// <summary>
    /// Record a counter metric (monotonically increasing value).
    /// </summary>
    void RecordCounter(string name, long value, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Record a gauge metric (point-in-time value).
    /// </summary>
    void RecordGauge(string name, double value, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Record a histogram metric (distribution of values).
    /// </summary>
    void RecordHistogram(string name, double value, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Flush all pending metrics immediately.
    /// </summary>
    Task FlushAsync();
}
