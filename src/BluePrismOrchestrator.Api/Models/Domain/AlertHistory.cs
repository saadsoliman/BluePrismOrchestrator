using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class AlertHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AlertRuleId { get; set; }
    public AlertRule? AlertRule { get; set; }

    [Required, MaxLength(200)]
    public string RuleName { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; }

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>The metric value that triggered the alert</summary>
    public double? TriggerValue { get; set; }

    /// <summary>The threshold that was breached</summary>
    public double? ThresholdValue { get; set; }

    [MaxLength(500)]
    public string? ProcessName { get; set; }

    [MaxLength(200)]
    public string? ResourceName { get; set; }

    /// <summary>Channels the alert was sent to</summary>
    public List<NotificationChannel> SentVia { get; set; } = new();

    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    [MaxLength(200)]
    public string? AcknowledgedBy { get; set; }

    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
}
