using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

/// <summary>
/// Defines a policy that stops specific Blue Prism process(es) when they exceed
/// a maximum runtime duration or cross a scheduled cutoff time window.
/// This is used to enforce application maintenance windows, license expiry cutoffs,
/// or SLA-driven execution constraints.
/// </summary>
public class CutoffPolicy
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Blue Prism process name — null means the policy applies to ALL processes</summary>
    [MaxLength(500)]
    public string? ProcessName { get; set; }

    /// <summary>Maximum allowed runtime in seconds before the process is force-stopped</summary>
    public int MaxRuntimeSeconds { get; set; } = 0;

    /// <summary>
    /// Daily cutoff time (UTC) after which any running session matching this policy
    /// will be stopped. 00:00 means the policy is duration-only (no time cutoff).
    /// </summary>
    public TimeSpan? CutoffTime { get; set; }

    /// <summary>Days of week the policy is active (bitfield: 1=Mon ... 64=Sun). 0 = every day.</summary>
    public int ActiveDays { get; set; } = 0;

    /// <summary>Whether the policy is currently enabled</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether to request a graceful stop or force-kill the Blue Prism session</summary>
    public bool ForceKill { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }

    /// <summary>Email addresses to notify when cutoff is triggered</summary>
    public List<string> NotifyEmails { get; set; } = new();
}

public class CutoffEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CutoffPolicyId { get; set; }
    public CutoffPolicy? Policy { get; set; }

    public Guid BPSessionId { get; set; }
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime TriggerTime { get; set; }
    public CutoffTriggerReason Reason { get; set; }
    public bool StopRequested { get; set; } = true;
    public string? ResultMessage { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public enum CutoffTriggerReason
{
    DurationExceeded,
    CutoffTimeReached
}