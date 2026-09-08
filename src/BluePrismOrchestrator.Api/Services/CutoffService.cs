using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Models.Domain;
using BluePrismOrchestrator.Api.Models.DTOs;
using BluePrismOrchestrator.Api.Models.BluePrism;
using BluePrismOrchestrator.Api.Services;

namespace BluePrismOrchestrator.Api.Services;

/// <summary>
/// Service that enforces cutoff policies on running Blue Prism sessions.
/// Monitors active sessions and force-stops those that exceed max runtime
/// or cross a configured cutoff time window.
/// </summary>
public interface ICutoffService
{
    Task<CutoffCheckResultDto> EvaluateCutoffsAsync();
    Task<List<CutoffEventDto>> GetCutoffHistoryAsync(int limit = 50);
    Task<CutoffPolicyDto> CreatePolicyAsync(CutoffPolicyDto dto);
    Task<CutoffPolicyDto?> GetPolicyAsync(Guid id);
    Task<List<CutoffPolicyDto>> GetAllPoliciesAsync();
    Task<CutoffPolicyDto> UpdatePolicyAsync(Guid id, CutoffPolicyDto dto);
    Task<bool> DeletePolicyAsync(Guid id);
    Task<CutoffPolicyDto> TogglePolicyAsync(Guid id, bool enabled);
}

public class CutoffService : ICutoffService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<CutoffService> _logger;

    public CutoffService(
        IServiceProvider serviceProvider,
        IHubContext<DashboardHub> hubContext,
        ILogger<CutoffService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all active cutoff policies against currently running BP sessions.
    /// Stops sessions that violate duration or cutoff-time rules.
    /// </summary>
    public async Task<CutoffCheckResultDto> EvaluateCutoffsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var bpReader = scope.ServiceProvider.GetRequiredService<BluePrismDbReader>();
        var automateC = scope.ServiceProvider.GetRequiredService<IAutomateCService>();

        var policies = await db.CutoffPolicies
            .Where(p => p.IsEnabled)
            .ToListAsync();

        var result = new CutoffCheckResultDto
        {
            PoliciesEvaluated = policies.Count
        };

        if (policies.Count == 0)
        {
            return result;
        }

        var activeSessions = (await bpReader.GetActiveSessionsAsync()).ToList();
        var now = DateTime.UtcNow;
        var today = now.DayOfWeek;

        foreach (var policy in policies)
        {
            // Check day-of-week filter
            if (policy.ActiveDays > 0)
            {
                var dayBit = 1 << ((int)today); // 1=Mon=1, 2=Tue=2, ... 0=Sun=64
                if ((policy.ActiveDays & dayBit) == 0)
                    continue;
            }

            foreach (var session in activeSessions)
            {
                // Filter by process name if policy is scoped
                if (policy.ProcessName != null &&
                    !session.ProcessName!.Equals(policy.ProcessName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var runtime = (now - (session.StartDateTime ?? now)).TotalSeconds;
                var matched = false;
                var reason = string.Empty;
                var stopped = false;
                var message = string.Empty;

                // Check 1: Duration exceeded
                if (policy.MaxRuntimeSeconds > 0 && runtime > policy.MaxRuntimeSeconds)
                {
                    matched = true;
                    reason = CutoffTriggerReason.DurationExceeded.ToString();
                }
                // Check 2: Cutoff time reached
                else if (policy.CutoffTime.HasValue && now.TimeOfDay >= policy.CutoffTime.Value)
                {
                    matched = true;
                    reason = CutoffTriggerReason.CutoffTimeReached.ToString();
                }

                if (!matched) continue;

                result.SessionsMatched++;

                // Stop the session
                ExecutionResult stopResult;
                if (policy.ForceKill)
                {
                    // Force kill via session ID if available
                    stopResult = await automateC.RequestStopBySessionIdAsync(session.SessionId);
                }
                else
                {
                    stopResult = await automateC.RequestStopAsync(
                        session.ProcessName ?? "Unknown",
                        session.ResourceName ?? "Unknown");
                }

                stopped = stopResult.Success;
                message = stopped
                    ? $"Stop request sent to Blue Prism for session {session.SessionId}"
                    : $"Stop request failed: {stopResult.Error}";

                // Record the cutoff event
                var cutoffEvent = new CutoffEvent
                {
                    CutoffPolicyId = policy.Id,
                    BPSessionId = session.SessionId,
                    ProcessName = session.ProcessName,
                    ResourceName = session.ResourceName,
                    SessionStartTime = session.StartDateTime ?? now,
                    TriggerTime = now,
                    Reason = (CutoffTriggerReason)Enum.Parse(typeof(CutoffTriggerReason), reason),
                    StopRequested = true,
                    ResultMessage = message,
                };
                db.CutoffEvents.Add(cutoffEvent);

                policy.LastTriggeredAt = now;
                policy.TriggerCount++;

                result.Details.Add(new CutoffSessionDetailDto
                {
                    SessionId = session.SessionId,
                    ProcessName = session.ProcessName ?? "Unknown",
                    ResourceName = session.ResourceName,
                    Reason = reason,
                    RuntimeSeconds = runtime,
                    Stopped = stopped,
                    Message = message
                });

                if (stopped)
                {
                    result.SessionsStopped++;

                    // Notify via SignalR
                    await _hubContext.Clients.All.SendAsync("CutoffTriggered", new
                    {
                        policyId = policy.Id,
                        policyName = policy.Name,
                        processName = session.ProcessName,
                        resourceName = session.ResourceName,
                        sessionId = session.SessionId,
                        reason = reason,
                        runtimeSeconds = runtime,
                        message = message,
                    });

                    _logger.LogWarning(
                        "Cutoff policy '{Policy}' stopped session {SessionId} for process '{Process}' after {Runtime}s (reason: {Reason})",
                        policy.Name, session.SessionId, session.ProcessName, runtime, reason);
                }

                // Send email notification if configured
                if (policy.NotifyEmails?.Count > 0)
                {
                    await SendCutoffNotificationAsync(policy, session, reason, runtime, stopped);
                }
            }
        }

        await db.SaveChangesAsync();
        return result;
    }

    private async Task SendCutoffNotificationAsync(
        CutoffPolicy policy, BPSession session, string reason, double runtime, bool stopped)
    {
        try
        {
            var status = stopped ? "STOPPED" : "FAILED TO STOP";
            var subject = $"[CUTOFF] {policy.Name} - {status}";
            var body = $"Cutoff policy '{policy.Name}' triggered.\n" +
                       $"Process: {session.ProcessName}\n" +
                       $"Resource: {session.ResourceName}\n" +
                       $"Session ID: {session.SessionId}\n" +
                       $"Runtime: {runtime:F0}s\n" +
                       $"Reason: {reason}\n" +
                       $"Action: {(policy.ForceKill ? "Force Kill" : "Graceful Stop")}\n" +
                       $"Status: {status}\n" +
                       $"Timestamp: {DateTime.UtcNow:u}";

            _logger.LogInformation("Cutoff notification queued for {Recipients}",
                string.Join(", ", policy.NotifyEmails ?? new()));

            // In production, send actual email via SMTP
            // For now, log and push via SignalR as an alert
            await _hubContext.Clients.All.SendAsync("AlertFired", new
            {
                id = Guid.NewGuid().ToString(),
                ruleName = $"Cutoff: {policy.Name}",
                severity = "Warning",
                message = body,
                processName = session.ProcessName,
                resourceName = session.ResourceName,
                time = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send cutoff notification for policy {Policy}", policy.Name);
        }
    }

    public async Task<List<CutoffEventDto>> GetCutoffHistoryAsync(int limit = 50)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var events = await db.CutoffEvents
            .OrderByDescending(e => e.TriggerTime)
            .Take(limit)
            .ToListAsync();

        return events.Select(e => new CutoffEventDto
        {
            Id = e.Id,
            PolicyId = e.CutoffPolicyId,
            ProcessName = e.ProcessName,
            ResourceName = e.ResourceName,
            SessionStartTime = e.SessionStartTime,
            TriggerTime = e.TriggerTime,
            Reason = e.Reason,
            StopRequested = e.StopRequested,
            ResultMessage = e.ResultMessage,
        }).ToList();
    }

    public async Task<CutoffPolicyDto> CreatePolicyAsync(CutoffPolicyDto dto)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policy = new CutoffPolicy
        {
            Name = dto.Name,
            Description = dto.Description,
            ProcessName = dto.ProcessName,
            MaxRuntimeSeconds = dto.MaxRuntimeSeconds,
            CutoffTime = dto.CutoffTime,
            ActiveDays = dto.ActiveDays,
            IsEnabled = dto.IsEnabled,
            ForceKill = dto.ForceKill,
            NotifyEmails = dto.NotifyEmails ?? new(),
        };

        db.CutoffPolicies.Add(policy);
        await db.SaveChangesAsync();

        dto.Id = policy.Id;
        return dto;
    }

    public async Task<CutoffPolicyDto?> GetPolicyAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policy = await db.CutoffPolicies.FindAsync(id);
        if (policy == null) return null;

        return new CutoffPolicyDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Description = policy.Description,
            ProcessName = policy.ProcessName,
            MaxRuntimeSeconds = policy.MaxRuntimeSeconds,
            CutoffTime = policy.CutoffTime,
            ActiveDays = policy.ActiveDays,
            IsEnabled = policy.IsEnabled,
            ForceKill = policy.ForceKill,
            NotifyEmails = policy.NotifyEmails,
        };
    }

    public async Task<List<CutoffPolicyDto>> GetAllPoliciesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policies = await db.CutoffPolicies
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return policies.Select(p => new CutoffPolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            ProcessName = p.ProcessName,
            MaxRuntimeSeconds = p.MaxRuntimeSeconds,
            CutoffTime = p.CutoffTime,
            ActiveDays = p.ActiveDays,
            IsEnabled = p.IsEnabled,
            ForceKill = p.ForceKill,
            NotifyEmails = p.NotifyEmails,
        }).ToList();
    }

    public async Task<CutoffPolicyDto> UpdatePolicyAsync(Guid id, CutoffPolicyDto dto)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policy = await db.CutoffPolicies.FindAsync(id);
        if (policy == null) throw new KeyNotFoundException($"Cutoff policy {id} not found");

        policy.Name = dto.Name;
        policy.Description = dto.Description;
        policy.ProcessName = dto.ProcessName;
        policy.MaxRuntimeSeconds = dto.MaxRuntimeSeconds;
        policy.CutoffTime = dto.CutoffTime;
        policy.ActiveDays = dto.ActiveDays;
        policy.IsEnabled = dto.IsEnabled;
        policy.ForceKill = dto.ForceKill;
        policy.NotifyEmails = dto.NotifyEmails ?? new();

        await db.SaveChangesAsync();

        dto.Id = policy.Id;
        return dto;
    }

    public async Task<bool> DeletePolicyAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policy = await db.CutoffPolicies.FindAsync(id);
        if (policy == null) return false;

        db.CutoffPolicies.Remove(policy);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CutoffPolicyDto> TogglePolicyAsync(Guid id, bool enabled)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        var policy = await db.CutoffPolicies.FindAsync(id);
        if (policy == null) throw new KeyNotFoundException($"Cutoff policy {id} not found");

        policy.IsEnabled = enabled;
        await db.SaveChangesAsync();

        return new CutoffPolicyDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Description = policy.Description,
            ProcessName = policy.ProcessName,
            MaxRuntimeSeconds = policy.MaxRuntimeSeconds,
            CutoffTime = policy.CutoffTime,
            ActiveDays = policy.ActiveDays,
            IsEnabled = policy.IsEnabled,
            ForceKill = policy.ForceKill,
            NotifyEmails = policy.NotifyEmails,
        };
    }
}