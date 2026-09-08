using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

/// <summary>
/// Defines a rule that automatically delivers process output artifacts
/// (e.g., generated Excel files) to Business stakeholders when a process completes.
/// </summary>
public class BusinessOutputRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Blue Prism process name this rule applies to</summary>
    [Required, MaxLength(500)]
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Output type to deliver — e.g., "Excel", "Pdf", "Csv"</summary>
    [MaxLength(50)]
    public string OutputType { get; set; } = "Excel";

    /// <summary>
    /// Glob pattern or filename to match output files (e.g., "*.xlsx", "Report_*.xlsx").
    /// Empty = match all files of the output type.
    /// </summary>
    [MaxLength(200)]
    public string? FileNamePattern { get; set; }

    /// <summary>
    /// Delivery method: "Email", "SharePoint", "TeamsChannel", "SmbShare".
    /// </summary>
    [MaxLength(50)]
    public string DeliveryMethod { get; set; } = "Email";

    /// <summary>
    /// Target path or email list depending on delivery method.
    /// For Email: comma-separated addresses.
    /// For SharePoint/SmbShare: full folder path.
    /// For TeamsChannel: webhook URL.
    /// </summary>
    [Required]
    public string DeliveryTarget { get; set; } = string.Empty;

    /// <summary>Whether to only deliver when the process succeeds, or also on failure</summary>
    public bool DeliverOnFailure { get; set; } = false;

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastDeliveredAt { get; set; }
}

public class BusinessOutputDeliveryLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BusinessOutputRuleId { get; set; }
    public BusinessOutputRule? Rule { get; set; }

    public Guid? BPSessionId { get; set; }
    [MaxLength(500)]
    public string? ProcessName { get; set; }
    [MaxLength(50)]
    public string? FileName { get; set; }
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    public DeliveryStatus Status { get; set; }
    public string? Message { get; set; }
}

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Skipped,
    Failed
}