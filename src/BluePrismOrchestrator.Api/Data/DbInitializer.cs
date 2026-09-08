using Microsoft.EntityFrameworkCore;
using BluePrismOrchestrator.Api.Models.Domain;

namespace BluePrismOrchestrator.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(OrchestratorDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Schedules.AnyAsync())
        {
            var sched1 = new OrchestratorSchedule
            {
                Name = "Morning Process Batch",
                ProcessName = "Generic_Worker",
                CronExpression = "0 8 * * 1-5",
                Priority = 8,
                SlaDeadline = TimeSpan.FromHours(2),
                AiOptimizationEnabled = true,
                MaxRetries = 3,
                BusinessHoursOnly = true,
                BusinessHoursStart = new TimeOnly(8, 0),
                BusinessHoursEnd = new TimeOnly(17, 0),
                IsEnabled = true
            };

            var sched2 = new OrchestratorSchedule
            {
                Name = "Daily Adjudication Run",
                ProcessName = "AI -POC",
                CronExpression = "30 9 * * 1-5",
                Priority = 7,
                SlaDeadline = TimeSpan.FromHours(3),
                AiOptimizationEnabled = true,
                MaxRetries = 2,
                IsEnabled = true
            };

            var sched3 = new OrchestratorSchedule
            {
                Name = "Nightly Debit Card Dispatcher",
                ProcessName = "Debit Card -Dispatcher",
                CronExpression = "0 23 * * *",
                Priority = 9,
                SlaDeadline = TimeSpan.FromHours(4),
                AiOptimizationEnabled = true,
                MaxRetries = 3,
                IsEnabled = true
            };

            var sched4 = new OrchestratorSchedule
            {
                Name = "Continuous DataRobot Example",
                ProcessName = "DataRobot - Example Process",
                CronExpression = "*/30 * * * *",
                Priority = 5,
                SlaDeadline = TimeSpan.FromMinutes(45),
                AiOptimizationEnabled = false,
                IsEnabled = true
            };

            db.Schedules.AddRange(sched1, sched2, sched3, sched4);
            await db.SaveChangesAsync();
        }

        if (!await db.AlertRules.AnyAsync())
        {
            db.AlertRules.AddRange(
                new AlertRule
                {
                    Name = "High Work Queue Backlog Alert",
                    Description = "Triggers if any work queue exceeds 100 pending items",
                    RuleType = AlertRuleType.Threshold,
                    Severity = AlertSeverity.Warning,
                    ThresholdValue = 100,
                    Operator = ComparisonOperator.GreaterThan,
                    MetricName = "PendingQueueItems",
                    Channels = new() { NotificationChannel.InApp },
                    IsEnabled = true
                },
                new AlertRule
                {
                    Name = "Runtime Bot Offline While Active",
                    Description = "Triggers when a runtime bot disconnects unexpectedly",
                    RuleType = AlertRuleType.ResourceDown,
                    Severity = AlertSeverity.Critical,
                    Channels = new() { NotificationChannel.InApp, NotificationChannel.Teams },
                    IsEnabled = true
                },
                new AlertRule
                {
                    Name = "3x Consecutive Process Failures",
                    Description = "Fires when the same process fails three times in succession",
                    RuleType = AlertRuleType.ConsecutiveFailures,
                    Severity = AlertSeverity.Critical,
                    ThresholdValue = 3,
                    Channels = new() { NotificationChannel.InApp, NotificationChannel.Email },
                    IsEnabled = true
                },
                new AlertRule
                {
                    Name = "AI Duration Anomaly Detection",
                    Description = "Fires when execution duration exceeds 2.5x baseline average",
                    RuleType = AlertRuleType.Anomaly,
                    Severity = AlertSeverity.Warning,
                    Channels = new() { NotificationChannel.InApp },
                    IsEnabled = true
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Executions.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var processes = new[] { "Generic_Worker", "AI -POC", "Debit Card -Dispatcher", "DataRobot - Example Process", "ADIB - Performer Template" };
            var resources = new[] { "FAMILYPC", "FAMILYPC_debug" };

            var execs = new List<ProcessExecution>();
            for (int i = 0; i < 40; i++)
            {
                var start = now.AddMinutes(-(i * 25 + 10));
                var dur = 95.0 + (i * 13 % 140);
                var isFailed = i == 5 || i == 18;

                execs.Add(new ProcessExecution
                {
                    ProcessName = processes[i % processes.Length],
                    ResourceName = resources[i % resources.Length],
                    Status = isFailed ? ExecutionStatus.Failed : ExecutionStatus.Completed,
                    StartedAt = start,
                    CompletedAt = start.AddSeconds(dur),
                    DurationSeconds = dur,
                    SlaDeadline = start.AddHours(2),
                    SlaBreached = isFailed || dur > 220,
                    ErrorMessage = isFailed ? "Exception in BP process step" : null,
                    AttemptNumber = 1,
                    PredictedDurationSeconds = 120.0,
                    PredictedFailureProbability = 0.05
                });
            }

            db.Executions.AddRange(execs);
            await db.SaveChangesAsync();
        }

        if (!await db.AlertHistory.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.AlertHistory.AddRange(
                new AlertHistory
                {
                    RuleName = "High Work Queue Backlog Alert",
                    Severity = AlertSeverity.Warning,
                    Message = "Queue 'Queue 2' backlog reached 128 pending items (threshold: 100)",
                    TriggerValue = 128,
                    ProcessName = "Generic_Worker",
                    FiredAt = now.AddMinutes(-25),
                    IsAcknowledged = false
                },
                new AlertHistory
                {
                    RuleName = "Runtime Bot Offline",
                    Severity = AlertSeverity.Critical,
                    Message = "Resource 'FAMILYPC' went OFFLINE unexpectedly",
                    ResourceName = "FAMILYPC",
                    FiredAt = now.AddHours(-1).AddMinutes(-10),
                    IsAcknowledged = true,
                    AcknowledgedBy = "SysAdmin"
                },
                new AlertHistory
                {
                    RuleName = "AI Duration Anomaly Detection",
                    Severity = AlertSeverity.Warning,
                    Message = "Process 'Debit Card -Dispatcher' runtime spiked to 412s (baseline: 135s, +205%)",
                    TriggerValue = 412,
                    ProcessName = "Debit Card -Dispatcher",
                    ResourceName = "FAMILYPC_debug",
                    FiredAt = now.AddHours(-3),
                    IsAcknowledged = false
                }
            );
            await db.SaveChangesAsync();
        }

        // ─── Cutoff Policies ──────────────────────────────────
        if (!await db.CutoffPolicies.AnyAsync())
        {
            db.CutoffPolicies.AddRange(
                new CutoffPolicy
                {
                    Name = "End-of-Day Process Cutoff",
                    Description = "Stops all running processes at 18:00 UTC to enforce application maintenance window",
                    MaxRuntimeSeconds = 0,
                    CutoffTime = new TimeSpan(18, 0, 0),
                    ActiveDays = 62, // Monday=1 ... Friday=31 (bitmask for Mon-Fri)
                    IsEnabled = true,
                    ForceKill = false,
                    NotifyEmails = new() { "noc@company.com", "bp-admin@company.com" },
                },
                new CutoffPolicy
                {
                    Name = "Max Runtime - Invoice_Processing",
                    Description = "Kills Invoice_Processing sessions that run longer than 600 seconds (10 min)",
                    ProcessName = "Invoice_Processing",
                    MaxRuntimeSeconds = 600,
                    IsEnabled = true,
                    ForceKill = true,
                    NotifyEmails = new() { "it-support@company.com" },
                },
                new CutoffPolicy
                {
                    Name = "Weekend Maintenance Window",
                    Description = "Stops ALL processes by 20:00 UTC on weekends for system maintenance",
                    MaxRuntimeSeconds = 0,
                    CutoffTime = new TimeSpan(20, 0, 0),
                    ActiveDays = (1 << 6) | 1, // Sat + Sun
                    IsEnabled = true,
                    ForceKill = false,
                    NotifyEmails = new() { "noc@company.com" },
                }
            );
            await db.SaveChangesAsync();
        }

        // ─── Business Output Rules ──────────────────────────────
        if (!await db.BusinessOutputRules.AnyAsync())
        {
            db.BusinessOutputRules.AddRange(
                new BusinessOutputRule
                {
                    Name = "Invoice Processing Excel Delivery",
                    Description = "Delivers generated invoice Excel files to the Finance team",
                    ProcessName = "Invoice_Processing",
                    OutputType = "Excel",
                    FileNamePattern = "*.xlsx",
                    DeliveryMethod = "Email",
                    DeliveryTarget = "finance@company.com,accounting@company.com",
                    DeliverOnFailure = false,
                    IsEnabled = true,
                },
                new BusinessOutputRule
                {
                    Name = "Claims Adjudication Output",
                    Description = "Delivers claims Excel output to the Business team share",
                    ProcessName = "Claims_Adjudication",
                    OutputType = "Excel",
                    FileNamePattern = "Claims_*.xlsx",
                    DeliveryMethod = "SmbShare",
                    DeliveryTarget = @"\\business-share\claims\",
                    DeliverOnFailure = false,
                    IsEnabled = true,
                },
                new BusinessOutputRule
                {
                    Name = "KYC Verification Report",
                    Description = "Delivers KYC verification results to compliance team",
                    ProcessName = "KYC_Verification",
                    OutputType = "Excel",
                    FileNamePattern = "*.xlsx",
                    DeliveryMethod = "Email",
                    DeliveryTarget = "compliance@company.com",
                    DeliverOnFailure = true,
                    IsEnabled = true,
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
