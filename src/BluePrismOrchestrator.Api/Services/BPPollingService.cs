using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using BluePrismOrchestrator.Api.Configuration;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Models.BluePrism;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;

namespace BluePrismOrchestrator.Api.Services;

/// <summary>
/// Background service that polls the Blue Prism database at regular intervals,
/// detects state changes, and pushes real-time updates via SignalR.
/// Also feeds resource utilization data for AI analytics.
/// </summary>
public class BPPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly int _pollingIntervalMs;
    private readonly ILogger<BPPollingService> _logger;

    // State tracking for change detection
    private Dictionary<Guid, int> _lastSessionStatuses = new();
    private HashSet<string> _lastOnlineResources = new();
    private Dictionary<string, int> _lastQueueDepths = new();
    private readonly Dictionary<string, double> _lastUtilizationSnapshot = new();
    private DateTime _lastUtilizationSnapshotTime = DateTime.MinValue;

    public BPPollingService(
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> hubContext,
        IOptions<BluePrismDbOptions> bpOptions,
        ILogger<BPPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _pollingIntervalMs = bpOptions.Value.PollingIntervalSeconds * 1000;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BP Polling Service started (interval: {Interval}ms)", _pollingIntervalMs);

        // Initial delay to let the application fully start
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bpReader = scope.ServiceProvider.GetRequiredService<BluePrismDbReader>();
                var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

                await PollSessionChangesAsync(bpReader);
                await PollResourceChangesAsync(bpReader, db);
                await PollQueueChangesAsync(bpReader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during BP polling cycle");
            }

            await Task.Delay(_pollingIntervalMs, stoppingToken);
        }
    }

    private async Task PollSessionChangesAsync(BluePrismDbReader bpReader)
    {
        var sessions = (await bpReader.GetActiveSessionsAsync()).ToList();
        var currentStatuses = sessions.ToDictionary(s => s.SessionId, s => s.StatusId);

        var changedSessions = new List<SessionDto>();

        foreach (var (sessionId, statusId) in currentStatuses)
        {
            if (!_lastSessionStatuses.TryGetValue(sessionId, out var lastStatus) || lastStatus != statusId)
            {
                var session = sessions.First(s => s.SessionId == sessionId);
                changedSessions.Add(new SessionDto
                {
                    SessionId = session.SessionId,
                    ProcessName = session.ProcessName ?? "Unknown",
                    ResourceName = session.ResourceName ?? "Unknown",
                    Status = session.StatusDescription ?? BPStatusMap.GetDescription(session.StatusId),
                    StartTime = session.StartDateTime,
                    EndTime = session.EndDateTime
                });
            }
        }

        // Detect sessions that completed (were active, now gone)
        var completedSessionIds = _lastSessionStatuses.Keys.Except(currentStatuses.Keys);
        // These are handled by the recent sessions query during the next dashboard refresh

        if (changedSessions.Count > 0)
        {
            await _hubContext.Clients.All.SendAsync("SessionsUpdated", changedSessions);
            _logger.LogDebug("Pushed {Count} session changes via SignalR", changedSessions.Count);
        }

        _lastSessionStatuses = currentStatuses;
    }

    private async Task PollResourceChangesAsync(BluePrismDbReader bpReader, OrchestratorDbContext db)
    {
        var resources = (await bpReader.GetResourcesAsync()).ToList();
        var currentOnline = resources.Where(r => r.IsOnline).Select(r => r.Name).ToHashSet();

        // Detect resource online/offline changes
        var newOnline = currentOnline.Except(_lastOnlineResources).ToList();
        var newOffline = _lastOnlineResources.Except(currentOnline).ToList();

        if (newOnline.Count > 0 || newOffline.Count > 0)
        {
            var resourceDtos = resources.Select(r => new ResourceStatusDto
            {
                Name = r.Name,
                IsOnline = r.IsOnline,
                ActiveSessions = r.ProcessesRunning ?? 0,
                PoolName = r.PoolName
            }).ToList();

            await _hubContext.Clients.All.SendAsync("ResourcesUpdated", resourceDtos);

            foreach (var name in newOnline)
                _logger.LogInformation("Resource {Name} came ONLINE", name);
            foreach (var name in newOffline)
                _logger.LogWarning("Resource {Name} went OFFLINE", name);
        }

        // Record utilization snapshots for analytics — throttled to once per minute and
        // only when utilization has changed by more than 5% since the last snapshot,
        // to avoid overwhelming the database with writes.
        var now = DateTime.UtcNow;
        var shouldSnapshot = now - _lastUtilizationSnapshotTime >= TimeSpan.FromMinutes(1);
        if (shouldSnapshot)
        {
            _lastUtilizationSnapshotTime = now;

            foreach (var resource in resources)
            {
                var util = resource.IsOnline ? Math.Min(100, (resource.ProcessesRunning ?? 0) * 35.0 + 10) : 0;
                var prev = _lastUtilizationSnapshot.GetValueOrDefault(resource.Name, -1);
                if (Math.Abs(util - prev) > 5.0)
                {
                    _lastUtilizationSnapshot[resource.Name] = util;
                    db.ResourceUtilizations.Add(new ResourceUtilization
                    {
                        ResourceName = resource.Name,
                        ActiveSessions = resource.ProcessesRunning ?? 0,
                        IsOnline = resource.IsOnline,
                        UtilizationPercent = util
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        _lastOnlineResources = currentOnline;
    }

    private async Task PollQueueChangesAsync(BluePrismDbReader bpReader)
    {
        var queues = (await bpReader.GetWorkQueueSummariesAsync()).ToList();
        var currentDepths = queues.ToDictionary(q => q.Name, q => q.PendingCount);

        var changed = false;
        foreach (var (name, depth) in currentDepths)
        {
            if (!_lastQueueDepths.TryGetValue(name, out var lastDepth) || lastDepth != depth)
            {
                changed = true;
                break;
            }
        }

        if (changed)
        {
            var queueDtos = queues.Select(q => new QueueSummaryDto
            {
                Name = q.Name,
                Pending = q.PendingCount,
                Locked = q.LockedCount,
                Completed = q.CompletedCount,
                Exceptioned = q.ExceptionedCount,
                Total = q.TotalCount,
                HealthPercent = q.TotalCount > 0
                    ? Math.Round((double)(q.CompletedCount) / q.TotalCount * 100, 1)
                    : 100
            }).ToList();

            await _hubContext.Clients.All.SendAsync("QueuesUpdated", queueDtos);
            _logger.LogDebug("Pushed queue depth changes via SignalR");
        }

        _lastQueueDepths = currentDepths;
    }
}
