using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class ProcessExecution
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Link to Blue Prism session ID (GUID from BPASession)</summary>
    public Guid? BPSessionId { get; set; }

    /// <summary>Link to the orchestrator schedule that triggered this</summary>
    public Guid? ScheduleId { get; set; }
    public OrchestratorSchedule? Schedule { get; set; }

    [Required, MaxLength(500)]
    public string ProcessName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ResourceName { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Queued;

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Duration in seconds</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>SLA deadline for this execution</summary>
    public DateTime? SlaDeadline { get; set; }

    public bool SlaBreached { get; set; }

    /// <summary>Attempt number (for retries)</summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>AI-predicted failure probability at time of scheduling</summary>
    public double? PredictedFailureProbability { get; set; }

    /// <summary>AI-predicted duration at time of scheduling</summary>
    public double? PredictedDurationSeconds { get; set; }

    /// <summary>Whether this was triggered by AI optimization or manual/cron schedule</summary>
    public bool AiTriggered { get; set; }
}

public enum ExecutionStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Terminated = 4,
    Stopped = 5,
    Retrying = 6,
    SlaBreached = 7
}
