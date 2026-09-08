namespace BluePrismOrchestrator.Api.Configuration;

public class AutomateCOptions
{
    public const string SectionName = "AutomateC";

    /// <summary>Full path to AutomateC.exe</summary>
    public string ExecutablePath { get; set; } = @"C:\Program Files\Blue Prism Limited\Blue Prism Automate\AutomateC.exe";

    /// <summary>Authentication mode: "SSO" or "Native"</summary>
    public string AuthMode { get; set; } = "SSO";

    /// <summary>Native auth username (only used when AuthMode = "Native")</summary>
    public string? Username { get; set; }

    /// <summary>Native auth password (only used when AuthMode = "Native")</summary>
    public string? Password { get; set; }

    /// <summary>Database connection name as configured in Blue Prism</summary>
    public string? DbConnectionName { get; set; }

    /// <summary>Maximum concurrent AutomateC.exe invocations</summary>
    public int MaxConcurrentInvocations { get; set; } = 5;

    /// <summary>Timeout in seconds for AutomateC.exe process execution</summary>
    public int TimeoutSeconds { get; set; } = 300;
}
