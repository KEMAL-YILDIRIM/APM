namespace APM.Collector.Configuration;

public class AzureStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string LogsTableName { get; set; } = "Logs";
    public string MetricsTableName { get; set; } = "Metrics";
    public string TracesTableName { get; set; } = "Traces";
    public string ApplicationsTableName { get; set; } = "Applications";
}
