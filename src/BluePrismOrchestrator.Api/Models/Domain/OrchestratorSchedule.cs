using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class OrchestratorSchedule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Blue Prism process name to execute</summary>
    [Required, MaxLength(500)]
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Cron expression for scheduling (e.g., "0 9 * * 1-5")</summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>Target resource name (null = auto-assign via workload balancer)</summary>
    [MaxLength(200)]
    public string? TargetResource { get; set; }

    /// <summary>Priority: 1 (highest) to 10 (lowest)</summary>
    public int Priority { get; set; } = 5;

    /// <summary>SLA deadline — process must complete by this time offset from schedule start</summary>
    public TimeSpan? SlaDeadline { get; set; }

    /// <summary>Whether AI optimization is enabled for this schedule</summary>
    public bool AiOptimizationEnabled { get; set; } = true;

    /// <summary>AI-suggested optimal execution window override</summary>
    public TimeSpan? AiSuggestedTimeOffset { get; set; }

    /// <summary>Max retry attempts on failure</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Retry delay in seconds (exponential backoff base)</summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>Business hours only execution</summary>
    public bool BusinessHoursOnly { get; set; }

    public TimeOnly BusinessHoursStart { get; set; } = new(8, 0);
    public TimeOnly BusinessHoursEnd { get; set; } = new(18, 0);

    /// <summary>Startup parameters as XML for /startp</summary>
    public string? StartupParametersXml { get; set; }

    /// <summary>Process names that must complete before this schedule runs</summary>
    public List<string> DependsOn { get; set; } = new();

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>Hangfire recurring job ID</summary>
    [MaxLength(200)]
    public string? HangfireJobId { get; set; }
}
