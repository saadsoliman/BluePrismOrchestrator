using System.ComponentModel.DataAnnotations;

namespace BluePrismOrchestrator.Api.Models.Domain;

public class ResourceUtilization
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string ResourceName { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Number of active sessions on this resource</summary>
    public int ActiveSessions { get; set; }

    /// <summary>Is the resource currently connected/online</summary>
    public bool IsOnline { get; set; }

    /// <summary>Utilization percentage (0-100)</summary>
    public double UtilizationPercent { get; set; }

    /// <summary>Queue items being processed by this resource</summary>
    public int QueueItemsProcessing { get; set; }
}
