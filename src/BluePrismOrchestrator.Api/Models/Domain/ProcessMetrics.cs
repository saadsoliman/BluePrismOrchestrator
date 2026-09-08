using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class ProcessMetrics
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string ProcessName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }

    /// <summary>Success rate (0.0 - 1.0)</summary>
    public double SuccessRate { get; set; }

    /// <summary>Average execution duration in seconds</summary>
    public double AvgDurationSeconds { get; set; }

    public double MinDurationSeconds { get; set; }
    public double MaxDurationSeconds { get; set; }

    /// <summary>Average queue wait time in seconds</summary>
    public double AvgQueueWaitSeconds { get; set; }

    /// <summary>Total items processed through work queues</summary>
    public int QueueItemsProcessed { get; set; }
    public int QueueItemsExceptioned { get; set; }

    public int SlaBreaches { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
