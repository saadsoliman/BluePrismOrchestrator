using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BluePrismOrchestrator.Api.Configuration;
using BluePrismOrchestrator.Api.Data;
using BluePrismOrchestrator.Api.Hubs;
using BluePrismOrchestrator.Api.Models.Domain;

namespace BluePrismOrchestrator.Api.Services;

public interface IAlertService
{
    Task EvaluateAlertRulesAsync();
    Task FireAlertAsync(AlertRule rule, string message, double? triggerValue = null, string? processName = null, string? resourceName = null);
    Task<IEnumerable<AlertHistory>> GetRecentAlertsAsync(int limit = 20);
    Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy);
}

public class AlertService : IAlertService
{
    private readonly OrchestratorDbContext _db;
    private readonly BluePrismDbReader _bpReader;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly AlertOptions _alertOptions;
    private readonly ILogger<AlertService> _logger;
    private readonly HttpClient _httpClient;

    public AlertService(
        OrchestratorDbContext db,
        BluePrismDbReader bpReader,
        IHubContext<DashboardHub> hubContext,
        IOptions<AlertOptions> alertOptions,
        ILogger<AlertService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _bpReader = bpReader;
        _hubContext = hubContext;
        _alertOptions = alertOptions.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("AlertClient");
    }

    public async Task EvaluateAlertRulesAsync()
    {
        var rules = await _db.AlertRules
            .Where(r => r.IsEnabled)
            .ToListAsync();

        foreach (var rule in rules)
        {
            try
            {
                // Check cooldown
                if (rule.LastTriggeredAt.HasValue &&
                    DateTime.UtcNow - rule.LastTriggeredAt.Value < TimeSpan.FromMinutes(rule.CooldownMinutes))
                {
                    continue;
                }

                var triggered = rule.RuleType switch
                {
                    AlertRuleType.Threshold => await EvaluateThresholdRuleAsync(rule),
                    AlertRuleType.QueueBacklog => await EvaluateQueueBacklogRuleAsync(rule),
                    AlertRuleType.ConsecutiveFailures => await EvaluateConsecutiveFailuresRuleAsync(rule),
                    _ => false
                };

                // Anomaly and SlaBreach rules are handled by AI engine and scheduler respectively
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alert rule {RuleId}: {Name}", rule.Id, rule.Name);
            }
        }
    }

    private async Task<bool> EvaluateThresholdRuleAsync(AlertRule rule)
    {
        if (rule.MetricName == null || rule.ThresholdValue == null || rule.Operator == null)
            return false;

        double? currentValue = rule.MetricName switch
        {
            "FailureRate" => await GetFailureRateAsync(rule.ProcessName, rule.TimeWindowMinutes ?? 60),
            "QueueDepth" => await GetQueueDepthAsync(rule.ProcessName),
            _ => null
        };

        if (currentValue == null) return false;

        var breached = rule.Operator switch
        {
            ComparisonOperator.GreaterThan => currentValue > rule.ThresholdValue,
            ComparisonOperator.LessThan => currentValue < rule.ThresholdValue,
            ComparisonOperator.GreaterThanOrEqual => currentValue >= rule.ThresholdValue,
            ComparisonOperator.LessThanOrEqual => currentValue <= rule.ThresholdValue,
            ComparisonOperator.Equal => Math.Abs(currentValue.Value - rule.ThresholdValue.Value) < 0.001,
            ComparisonOperator.NotEqual => Math.Abs(currentValue.Value - rule.ThresholdValue.Value) >= 0.001,
            _ => false
        };

        if (breached)
        {
            var message = $"{rule.MetricName} is {currentValue:F2} (threshold: {rule.Operator} {rule.ThresholdValue:F2})";
            if (rule.ProcessName != null) message += $" for process '{rule.ProcessName}'";

            await FireAlertAsync(rule, message, currentValue, rule.ProcessName, rule.ResourceName);
        }

        return breached;
    }

    private async Task<bool> EvaluateQueueBacklogRuleAsync(AlertRule rule)
    {
        var queues = await _bpReader.GetWorkQueueSummariesAsync();
        var backloggedQueues = queues.Where(q =>
            q.PendingCount > (rule.ThresholdValue ?? 100)).ToList();

        if (backloggedQueues.Count > 0)
        {
            foreach (var q in backloggedQueues)
            {
                var message = $"Queue '{q.Name}' backlog: {q.PendingCount} pending items (threshold: {rule.ThresholdValue})";
                await FireAlertAsync(rule, message, q.PendingCount, rule.ProcessName, rule.ResourceName);
            }
            return true;
        }
        return false;
    }

    private async Task<bool> EvaluateConsecutiveFailuresRuleAsync(AlertRule rule)
    {
        var threshold = (int)(rule.ThresholdValue ?? 3);
        var recentExecutions = await _db.Executions
            .Where(e => rule.ProcessName == null || e.ProcessName == rule.ProcessName)
            .OrderByDescending(e => e.CompletedAt)
            .Take(threshold)
            .ToListAsync();

        if (recentExecutions.Count >= threshold && recentExecutions.All(e => e.Status == ExecutionStatus.Failed))
        {
            var processName = rule.ProcessName ?? recentExecutions.First().ProcessName;
            var message = $"{threshold} consecutive failures for process '{processName}'";
            await FireAlertAsync(rule, message, threshold, processName, rule.ResourceName);
            return true;
        }
        return false;
    }

    public async Task FireAlertAsync(AlertRule rule, string message, double? triggerValue = null, string? processName = null, string? resourceName = null)
    {
        _logger.LogWarning("ALERT [{Severity}] {Name}: {Message}", rule.Severity, rule.Name, message);

        // Record in history
        var history = new AlertHistory
        {
            AlertRuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Message = message,
            TriggerValue = triggerValue,
            ThresholdValue = rule.ThresholdValue,
            ProcessName = processName,
            ResourceName = resourceName,
            SentVia = rule.Channels
        };
        _db.AlertHistory.Add(history);

        // Update rule stats
        rule.LastTriggeredAt = DateTime.UtcNow;
        rule.TriggerCount++;
        await _db.SaveChangesAsync();

        // Send to channels
        foreach (var channel in rule.Channels)
        {
            try
            {
                switch (channel)
                {
                    case NotificationChannel.InApp:
                        await SendInAppNotificationAsync(history);
                        break;
                    case NotificationChannel.Email:
                        await SendEmailNotificationAsync(rule, history);
                        break;
                    case NotificationChannel.Teams:
                        await SendTeamsNotificationAsync(history);
                        break;
                    case NotificationChannel.Webhook:
                        await SendWebhookNotificationAsync(history);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert via {Channel}", channel);
            }
        }
    }

    private async Task SendInAppNotificationAsync(AlertHistory alert)
    {
        await _hubContext.Clients.All.SendAsync("AlertFired", new
        {
            alert.Id,
            alert.RuleName,
            Severity = alert.Severity.ToString(),
            alert.Message,
            alert.ProcessName,
            alert.ResourceName,
            alert.FiredAt
        });
    }

    private async Task SendEmailNotificationAsync(AlertRule rule, AlertHistory alert)
    {
        if (_alertOptions.Smtp == null || rule.EmailRecipients.Count == 0) return;

        using var client = new SmtpClient(_alertOptions.Smtp.Host, _alertOptions.Smtp.Port)
        {
            Credentials = new NetworkCredential(_alertOptions.Smtp.Username, _alertOptions.Smtp.Password),
            EnableSsl = _alertOptions.Smtp.UseSsl
        };

        var subject = $"[{alert.Severity}] BP Orchestrator Alert: {alert.RuleName}";
        var body = $"Alert: {alert.RuleName}\nSeverity: {alert.Severity}\n\n{alert.Message}\n\nFired at: {alert.FiredAt:u}";

        foreach (var recipient in rule.EmailRecipients)
        {
            var mailMessage = new MailMessage(_alertOptions.Smtp.FromAddress, recipient, subject, body);
            await client.SendMailAsync(mailMessage);
        }

        _logger.LogInformation("Sent email alert to {Count} recipients", rule.EmailRecipients.Count);
    }

    private async Task SendTeamsNotificationAsync(AlertHistory alert)
    {
        if (_alertOptions.Teams == null || string.IsNullOrEmpty(_alertOptions.Teams.WebhookUrl)) return;

        var severityColor = alert.Severity switch
        {
            AlertSeverity.Critical => "FF0000",
            AlertSeverity.Warning => "FFA500",
            _ => "0078D4"
        };

        var card = new
        {
            type = "message",
            attachments = new[] { new {
                contentType = "application/vnd.microsoft.card.adaptive",
                content = new {
                    type = "AdaptiveCard",
                    version = "1.2",
                    body = new object[] {
                        new { type = "TextBlock", text = $"🚨 {alert.RuleName}", weight = "Bolder", size = "Medium", color = "Attention" },
                        new { type = "TextBlock", text = alert.Message, wrap = true },
                        new { type = "FactSet", facts = new[] {
                            new { title = "Severity", value = alert.Severity.ToString() },
                            new { title = "Process", value = alert.ProcessName ?? "N/A" },
                            new { title = "Resource", value = alert.ResourceName ?? "N/A" },
                            new { title = "Time", value = alert.FiredAt.ToString("u") }
                        }}
                    }
                }
            }}
        };

        var json = JsonSerializer.Serialize(card);
        await _httpClient.PostAsync(
            _alertOptions.Teams.WebhookUrl,
            new StringContent(json, Encoding.UTF8, "application/json"));

        _logger.LogInformation("Sent Teams notification for alert {AlertId}", alert.Id);
    }

    private async Task SendWebhookNotificationAsync(AlertHistory alert)
    {
        if (_alertOptions.Webhook == null || string.IsNullOrEmpty(_alertOptions.Webhook.Url)) return;

        var payload = JsonSerializer.Serialize(new
        {
            alert.Id,
            alert.RuleName,
            Severity = alert.Severity.ToString(),
            alert.Message,
            alert.ProcessName,
            alert.ResourceName,
            alert.TriggerValue,
            alert.ThresholdValue,
            alert.FiredAt
        });

        var request = new HttpRequestMessage(HttpMethod.Post, _alertOptions.Webhook.Url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        foreach (var (key, value) in _alertOptions.Webhook.Headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }

        await _httpClient.SendAsync(request);
        _logger.LogInformation("Sent webhook notification for alert {AlertId}", alert.Id);
    }

    private async Task<double> GetFailureRateAsync(string? processName, int windowMinutes)
    {
        var since = DateTime.UtcNow.AddMinutes(-windowMinutes);
        var executions = await _db.Executions
            .Where(e => e.CompletedAt >= since)
            .Where(e => processName == null || e.ProcessName == processName)
            .Where(e => e.Status == ExecutionStatus.Completed || e.Status == ExecutionStatus.Failed)
            .ToListAsync();

        if (executions.Count == 0) return 0;
        return (double)executions.Count(e => e.Status == ExecutionStatus.Failed) / executions.Count;
    }

    private async Task<double> GetQueueDepthAsync(string? queueName)
    {
        var queues = await _bpReader.GetWorkQueueSummariesAsync();
        if (queueName != null)
        {
            var queue = queues.FirstOrDefault(q => q.Name == queueName);
            return queue?.PendingCount ?? 0;
        }
        return queues.Sum(q => q.PendingCount);
    }

    public async Task<IEnumerable<AlertHistory>> GetRecentAlertsAsync(int limit = 20)
    {
        return await _db.AlertHistory
            .OrderByDescending(a => a.FiredAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy)
    {
        var alert = await _db.AlertHistory.FindAsync(alertId);
        if (alert != null)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = acknowledgedBy;
            await _db.SaveChangesAsync();
        }
    }
}
