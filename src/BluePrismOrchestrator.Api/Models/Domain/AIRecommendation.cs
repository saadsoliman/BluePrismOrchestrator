using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class AIRecommendation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public RecommendationType Type { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Confidence score 0.0 - 1.0</summary>
    public double Confidence { get; set; }

    /// <summary>Estimated impact description (e.g., "Could reduce failures by 35%")</summary>
    [MaxLength(500)]
    public string? EstimatedImpact { get; set; }

    [MaxLength(500)]
    public string? ProcessName { get; set; }

    [MaxLength(200)]
    public string? ResourceName { get; set; }

    /// <summary>Serialized action data (JSON) for implementing the recommendation</summary>
    public string? ActionPayload { get; set; }

    public RecommendationStatus Status { get; set; } = RecommendationStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ActionedAt { get; set; }
}

public enum RecommendationType
{
    ResourceReassignment,
    ScheduleOptimization,
    CapacityScaling,
    ProcessConfiguration,
    QueueManagement,
    FailurePrevention
}

public enum RecommendationStatus
{
    Active,
    Accepted,
    Dismissed,
    Expired,
    Implemented
}
