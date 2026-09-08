using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Services.AI;

public interface IAnomalyDetectionService
{
    Task<List<AnomalyDto>> DetectAnomaliesAsync();
    Task<List<AnomalyDto>> GetRecentAnomaliesAsync(int limit = 20);
}

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<AnomalyDetectionService> _logger;

    // Cache of recent anomalies in memory
    private static readonly List<AnomalyDto> _recentAnomalies = new();
    private static readonly object _lock = new();

    public AnomalyDetectionService(
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> hubContext,
        ILogger<AnomalyDetectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<List<AnomalyDto>> GetRecentAnomaliesAsync(int limit = 20)
    {
        lock (_lock)
        {
            if (_recentAnomalies.Count > 0)
            {
                return _recentAnomalies.Take(limit).ToList();
            }
        }

        // If cache empty, run detection
        return await DetectAnomaliesAsync();
    }

    public async Task<List<AnomalyDto>> DetectAnomaliesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var bpReader = scope.ServiceProvider.GetRequiredService<BluePrismDbReader>();

        var detected = new List<AnomalyDto>();
        var now = DateTime.UtcNow;

        try
        {
            // 1. Duration Outlier Detection (Executions exceeding 2.5x mean duration)
            var recentCompletedExecutions = await db.Executions
                .Where(e => e.CompletedAt != null && e.StartedAt >= now.AddHours(-12) && e.DurationSeconds > 0)
                .ToListAsync();

            var processGroups = recentCompletedExecutions.GroupBy(e => e.ProcessName);

            foreach (var group in processGroups)
            {
                var durations = group.Where(e => e.DurationSeconds.HasValue).Select(e => e.DurationSeconds!.Value).ToList();
                if (durations.Count >= 3)
                {
                    var avg = durations.Average();
                    var variance = durations.Select(d => Math.Pow(d - avg, 2)).Average();
                    var stdDev = Math.Sqrt(variance);

                    var outlierThreshold = avg + Math.Max(stdDev * 2.2, avg * 0.75);

                    foreach (var exec in group.Where(e => (e.DurationSeconds ?? 0) > outlierThreshold))
                    {
                        var duration = exec.DurationSeconds!.Value;
                        var deviationPct = Math.Round(((duration - avg) / avg) * 100, 1);
                        detected.Add(new AnomalyDto
                        {
                            Id = Guid.NewGuid(),
                            Type = "DurationSpike",
                            Description = $"Process execution took {duration:F0}s (baseline avg: {avg:F0}s, +{deviationPct}%)",
                            Severity = deviationPct > 150 ? AlertSeverity.Critical : AlertSeverity.Warning,
                            DeviationPercent = deviationPct,
                            ProcessName = exec.ProcessName,
                            ResourceName = exec.ResourceName,
                            DetectedAt = exec.CompletedAt ?? now
                        });
                    }
                }
            }

            // 2. Work Queue Accumulation Spikes
            var queues = await bpReader.GetWorkQueueSummariesAsync();
            foreach (var q in queues)
            {
                if (q.PendingCount > 150)
                {
                    detected.Add(new AnomalyDto
                    {
                        Id = Guid.NewGuid(),
                        Type = "QueueSurge",
                        Description = $"Queue '{q.Name}' has abnormal backlog surge of {q.PendingCount} items waiting",
                        Severity = q.PendingCount > 300 ? AlertSeverity.Critical : AlertSeverity.Warning,
                        DeviationPercent = Math.Round((double)q.PendingCount / 50 * 100, 1),
                        ProcessName = q.Name,
                        DetectedAt = now
                    });
                }
            }

            // 3. Resource Dropouts / Sudden Offline during active session
            var resources = await bpReader.GetResourcesAsync();
            var offlineWithActiveProcesses = resources.Where(r => !r.IsOnline && (r.ProcessesRunning ?? 0) > 0).ToList();
            foreach (var r in offlineWithActiveProcesses)
            {
                detected.Add(new AnomalyDto
                {
                    Id = Guid.NewGuid(),
                    Type = "ResourceDrop",
                    Description = $"Resource '{r.Name}' abruptly disconnected while executing {r.ProcessesRunning} active process(es)",
                    Severity = AlertSeverity.Critical,
                    DeviationPercent = 100,
                    ResourceName = r.Name,
                    DetectedAt = now
                });
            }

            // 4. Duration outlier detection for resources currently offline-but-running
            //    (kept intentionally empty — real anomalies are the source of truth)
            //    Removed synthetic fallback that injected phantom process names.

            lock (_lock)
            {
                _recentAnomalies.Clear();
                _recentAnomalies.AddRange(detected);
            }

            await _hubContext.Clients.All.SendAsync("AnomaliesUpdated", detected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting operational anomalies");
        }

        return detected;
    }
}
