using Hangfire;
using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Models.Domain;

namespace BluePrismOrchestrator.Api.Services;

public interface ISmartSchedulerService
{
    Task RegisterScheduleAsync(OrchestratorSchedule schedule);
    Task UnregisterScheduleAsync(Guid scheduleId);
    Task ExecuteScheduledProcessAsync(Guid scheduleId);
    Task SyncAllSchedulesAsync();
    Task<bool> AreDependenciesMet(OrchestratorSchedule schedule);
}

public class SmartSchedulerService : ISmartSchedulerService
{
    private readonly OrchestratorDbContext _db;
    private readonly IAutomateCService _automateC;
    private readonly IWorkloadBalancerService _workloadBalancer;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<SmartSchedulerService> _logger;

    public SmartSchedulerService(
        OrchestratorDbContext db,
        IAutomateCService automateC,
        IWorkloadBalancerService workloadBalancer,
        IRecurringJobManager recurringJobs,
        IBackgroundJobClient backgroundJobs,
        ILogger<SmartSchedulerService> logger)
    {
        _db = db;
        _automateC = automateC;
        _workloadBalancer = workloadBalancer;
        _recurringJobs = recurringJobs;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task RegisterScheduleAsync(OrchestratorSchedule schedule)
    {
        if (string.IsNullOrEmpty(schedule.CronExpression))
        {
            _logger.LogWarning("Schedule {Id} has no cron expression, skipping registration", schedule.Id);
            return;
        }

        var jobId = $"orchestrator-{schedule.Id}";
        schedule.HangfireJobId = jobId;

        if (schedule.IsEnabled)
        {
            _recurringJobs.AddOrUpdate(
                jobId,
                () => ExecuteScheduledProcessAsync(schedule.Id),
                schedule.CronExpression,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

            _logger.LogInformation(
                "Registered schedule {Name} ({Id}) with cron '{Cron}'",
                schedule.Name, schedule.Id, schedule.CronExpression);
        }
        else
        {
            _recurringJobs.RemoveIfExists(jobId);
            _logger.LogInformation("Schedule {Name} ({Id}) is disabled, removed from Hangfire", schedule.Name, schedule.Id);
        }

        _db.Schedules.Update(schedule);
        await _db.SaveChangesAsync();
    }

    public async Task UnregisterScheduleAsync(Guid scheduleId)
    {
        var jobId = $"orchestrator-{scheduleId}";
        _recurringJobs.RemoveIfExists(jobId);
        _logger.LogInformation("Unregistered schedule {Id} from Hangfire", scheduleId);
        await Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 0)] // We handle retries ourselves
    public async Task ExecuteScheduledProcessAsync(Guid scheduleId)
    {
        var schedule = await _db.Schedules.FindAsync(scheduleId);
        if (schedule == null || !schedule.IsEnabled)
        {
            _logger.LogWarning("Schedule {Id} not found or disabled, skipping execution", scheduleId);
            return;
        }

        // Check business hours
        if (schedule.BusinessHoursOnly)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            if (now < schedule.BusinessHoursStart || now > schedule.BusinessHoursEnd)
            {
                _logger.LogInformation(
                    "Schedule {Name} restricted to business hours ({Start}-{End}), current time {Now} is outside window",
                    schedule.Name, schedule.BusinessHoursStart, schedule.BusinessHoursEnd, now);
                return;
            }
        }

        // Check dependencies
        if (!await AreDependenciesMet(schedule))
        {
            _logger.LogInformation(
                "Schedule {Name} has unmet dependencies: {Deps}, deferring execution",
                schedule.Name, string.Join(", ", schedule.DependsOn));
            // Re-queue with a delay
            _backgroundJobs.Schedule(
                () => ExecuteScheduledProcessAsync(scheduleId),
                TimeSpan.FromMinutes(5));
            return;
        }

        // Determine target resource via workload balancer
        var targetResource = schedule.TargetResource
            ?? await _workloadBalancer.GetOptimalResourceAsync(schedule.ProcessName);

        // Create execution record
        var execution = new ProcessExecution
        {
            ScheduleId = scheduleId,
            ProcessName = schedule.ProcessName,
            ResourceName = targetResource,
            Status = ExecutionStatus.Queued,
            SlaDeadline = schedule.SlaDeadline.HasValue
                ? DateTime.UtcNow.Add(schedule.SlaDeadline.Value)
                : null
        };
        _db.Executions.Add(execution);
        await _db.SaveChangesAsync();

        // Execute with retry logic
        await ExecuteWithRetryAsync(execution, schedule);
    }

    private async Task ExecuteWithRetryAsync(ProcessExecution execution, OrchestratorSchedule schedule)
    {
        for (int attempt = 1; attempt <= schedule.MaxRetries; attempt++)
        {
            execution.AttemptNumber = attempt;
            execution.Status = ExecutionStatus.Running;
            execution.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Executing {Process} on {Resource} (attempt {Attempt}/{Max})",
                execution.ProcessName, execution.ResourceName, attempt, schedule.MaxRetries);

            // Parse startup parameters
            Dictionary<string, string>? parameters = null;
            // In a real implementation, parse schedule.StartupParametersXml here

            var result = await _automateC.RunProcessAsync(
                execution.ProcessName,
                execution.ResourceName,
                parameters);

            if (result.Success)
            {
                execution.Status = ExecutionStatus.Completed;
                execution.CompletedAt = DateTime.UtcNow;
                execution.DurationSeconds = (execution.CompletedAt.Value - execution.StartedAt.Value).TotalSeconds;

                // Check SLA
                if (execution.SlaDeadline.HasValue && execution.CompletedAt > execution.SlaDeadline)
                {
                    execution.SlaBreached = true;
                    _logger.LogWarning("SLA breached for {Process}: completed at {Completed}, deadline was {Deadline}",
                        execution.ProcessName, execution.CompletedAt, execution.SlaDeadline);
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Execution {Id} completed successfully in {Duration}s",
                    execution.Id, execution.DurationSeconds);
                return;
            }

            // Failed — log and retry
            execution.ErrorMessage = result.Error;
            _logger.LogWarning(
                "Execution {Id} failed on attempt {Attempt}: {Error}",
                execution.Id, attempt, result.Error);

            if (attempt < schedule.MaxRetries)
            {
                execution.Status = ExecutionStatus.Retrying;
                await _db.SaveChangesAsync();

                var delay = TimeSpan.FromSeconds(schedule.RetryDelaySeconds * Math.Pow(2, attempt - 1));
                _logger.LogInformation("Retrying in {Delay}", delay);
                await Task.Delay(delay);
            }
        }

        // All retries exhausted
        execution.Status = ExecutionStatus.Failed;
        execution.CompletedAt = DateTime.UtcNow;
        execution.DurationSeconds = (execution.CompletedAt.Value - (execution.StartedAt ?? execution.QueuedAt)).TotalSeconds;
        await _db.SaveChangesAsync();

        _logger.LogError("Execution {Id} for {Process} failed after {Max} attempts",
            execution.Id, execution.ProcessName, schedule.MaxRetries);
    }

    public async Task<bool> AreDependenciesMet(OrchestratorSchedule schedule)
    {
        if (schedule.DependsOn.Count == 0) return true;

        var today = DateTime.UtcNow.Date;
        foreach (var dep in schedule.DependsOn)
        {
            var completed = await _db.Executions.AnyAsync(e =>
                e.ProcessName == dep
                && e.Status == ExecutionStatus.Completed
                && e.CompletedAt >= today);

            if (!completed) return false;
        }
        return true;
    }

    public async Task SyncAllSchedulesAsync()
    {
        var schedules = await _db.Schedules.Where(s => s.IsEnabled).ToListAsync();
        foreach (var schedule in schedules)
        {
            await RegisterScheduleAsync(schedule);
        }
        _logger.LogInformation("Synced {Count} active schedules with Hangfire", schedules.Count);
    }
}
