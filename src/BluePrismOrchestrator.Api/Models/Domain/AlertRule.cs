using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class AlertRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public AlertRuleType RuleType { get; set; }

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>Target process name (null = applies to all processes)</summary>
    [MaxLength(500)]
    public string? ProcessName { get; set; }

    /// <summary>Target resource name (null = applies to all resources)</summary>
    [MaxLength(200)]
    public string? ResourceName { get; set; }

    /// <summary>Threshold value (e.g., queue depth > 100, failure rate > 0.2)</summary>
    public double? ThresholdValue { get; set; }

    /// <summary>Comparison operator for threshold</summary>
    public ComparisonOperator? Operator { get; set; }

    /// <summary>Metric to monitor (e.g., "QueueDepth", "FailureRate", "Duration", "ResourceUptime")</summary>
    [MaxLength(100)]
    public string? MetricName { get; set; }

    /// <summary>Time window in minutes for rate/pattern calculations</summary>
    public int? TimeWindowMinutes { get; set; }

    /// <summary>Cooldown period in minutes to avoid alert storms</summary>
    public int CooldownMinutes { get; set; } = 15;

    /// <summary>Notification channels enabled for this rule</summary>
    public List<NotificationChannel> Channels { get; set; } = new() { NotificationChannel.InApp };

    /// <summary>Email recipients for email channel</summary>
    public List<string> EmailRecipients { get; set; } = new();

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }
}

public enum AlertRuleType
{
    Threshold,
    Anomaly,
    SlaBreach,
    ResourceDown,
    ConsecutiveFailures,
    QueueBacklog
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual
}

public enum NotificationChannel
{
    InApp,
    Email,
    Teams,
    Webhook
}
