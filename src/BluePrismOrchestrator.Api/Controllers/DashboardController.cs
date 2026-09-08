using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Models.BluePrism;

namespace BluePrismOrchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly OrchestratorDbContext _db;
    private readonly BluePrismDbReader _bpReader;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        OrchestratorDbContext db,
        BluePrismDbReader bpReader,
        ILogger<DashboardController> logger)
    {
        _db = db;
        _bpReader = bpReader;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        try
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;

            // BP Resources
            var bpResources = (await _bpReader.GetResourcesAsync()).ToList();
            var totalResources = bpResources.Count;
            var onlineResources = bpResources.Count(r => r.IsOnline);

            // Active Sessions
            var activeSessions = (await _bpReader.GetActiveSessionsAsync()).ToList();

            // Total BP Processes
            var bpProcesses = (await _bpReader.GetProcessesAsync()).ToList();

            // Work Queues
            var queues = (await _bpReader.GetWorkQueueSummariesAsync()).ToList();
            var pendingQueueItems = queues.Sum(q => q.PendingCount);

            // Executions today from Orchestrator DB
            var executionsToday = await _db.Executions
                .Where(e => e.StartedAt >= todayStart)
                .ToListAsync();

            var totalExecToday = executionsToday.Count;
            var completedToday = executionsToday.Count(e => e.Status == ExecutionStatus.Completed);
            var successRate = totalExecToday > 0
                ? Math.Round((double)completedToday / totalExecToday * 100, 1)
                : await _bpReader.GetSuccessRate24hAsync();

            // Avg queue wait from real queue depths if BP has data, else 0
            var avgQueueWait = queues.Any(q => q.TotalCount > 0)
                ? Math.Round(queues.Average(q => q.PendingCount * 1.5), 1)
                : 0.0;

            // Alerts
            var recentAlerts = await _db.AlertHistory
                .OrderByDescending(a => a.FiredAt)
                .Take(10)
                .Select(a => new AlertHistoryDto
                {
                    Id = a.Id,
                    RuleName = a.RuleName,
                    Severity = a.Severity,
                    Message = a.Message,
                    TriggerValue = a.TriggerValue,
                    ProcessName = a.ProcessName,
                    ResourceName = a.ResourceName,
                    IsAcknowledged = a.IsAcknowledged,
                    FiredAt = a.FiredAt
                })
                .ToListAsync();

            var resourceDtos = bpResources.Select(r => new ResourceStatusDto
            {
                Name = r.Name,
                IsOnline = r.IsOnline,
                ActiveSessions = r.ProcessesRunning ?? 0,
                UtilizationPercent = r.IsOnline ? Math.Min(100, (r.ProcessesRunning ?? 0) * 35.0 + 10) : 0,
                PoolName = r.PoolName
            }).ToList();

            var sessionDtos = activeSessions.Select(s => new SessionDto
            {
                SessionId = s.SessionId,
                ProcessName = s.ProcessName ?? "Unknown",
                ResourceName = s.ResourceName ?? "Unknown",
                Status = s.StatusDescription ?? BPStatusMap.GetDescription(s.StatusId),
                StartTime = s.StartDateTime,
                EndTime = s.EndDateTime,
                DurationSeconds = s.StartDateTime.HasValue ? (now - s.StartDateTime.Value).TotalSeconds : null
            }).ToList();

            var queueDtos = queues.Select(q => new QueueSummaryDto
            {
                Name = q.Name,
                Pending = q.PendingCount,
                Locked = q.LockedCount,
                Completed = q.CompletedCount,
                Exceptioned = q.ExceptionedCount,
                Total = q.TotalCount,
                HealthPercent = q.TotalCount > 0
                    ? Math.Round((double)q.CompletedCount / q.TotalCount * 100, 1)
                    : 100
            }).ToList();

            return Ok(new DashboardSummaryDto
            {
                TotalResources = totalResources,
                OnlineResources = onlineResources,
                ActiveSessions = activeSessions.Count,
                TotalProcesses = bpProcesses.Count,
                SuccessRate24h = successRate,
                AvgQueueWaitSeconds = avgQueueWait,
                PendingQueueItems = pendingQueueItems,
                 TotalExecutionsToday = totalExecToday,
                 Resources = resourceDtos,
                 ActiveSessionsList = sessionDtos,
                 Queues = queueDtos,
                 RecentAlerts = recentAlerts
             });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard summary");
            return StatusCode(500, "Failed to retrieve dashboard summary");
        }
    }
}
