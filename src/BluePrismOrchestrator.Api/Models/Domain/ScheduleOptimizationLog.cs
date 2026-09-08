using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class ScheduleOptimizationLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ScheduleId { get; set; }
    public OrchestratorSchedule? Schedule { get; set; }

    [Required, MaxLength(500)]
    public string ProcessName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OptimizationType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? OriginalValue { get; set; }

    [MaxLength(200)]
    public string? OptimizedValue { get; set; }

    [MaxLength(2000)]
    public string? Reasoning { get; set; }

    public double ConfidenceScore { get; set; }

    public bool WasApplied { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
