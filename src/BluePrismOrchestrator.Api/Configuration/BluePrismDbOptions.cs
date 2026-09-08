namespace BluePrismOrchestrator.Api.Configuration;

public class BluePrismDbOptions
{
    public const string SectionName = "BluePrismDb";

    /// <summary>SQL Server connection string to the Blue Prism database (read-only access)</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Polling interval in seconds for BP database changes</summary>
    public int PollingIntervalSeconds { get; set; } = 10;
}
