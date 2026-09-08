using BluePrismOrchestrator.Api.Models.Domain;

namespace BluePrismOrchestrator.Api.Models.DTOs;

// ─── Dashboard ───────────────────────────────────────────────
public class DashboardSummaryDto
{
    public int TotalResources { get; set; }
    public int OnlineResources { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalProcesses { get; set; }
    public double SuccessRate24h { get; set; }
    public double AvgQueueWaitSeconds { get; set; }
    public int PendingQueueItems { get; set; }
    public int TotalExecutionsToday { get; set; }
    public List<ResourceStatusDto> Resources { get; set; } = new();
    public List<SessionDto> ActiveSessionsList { get; set; } = new();
    public List<QueueSummaryDto> Queues { get; set; } = new();
    public List<AlertHistoryDto> RecentAlerts { get; set; } = new();
}

public class ResourceStatusDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int ActiveSessions { get; set; }
    public double UtilizationPercent { get; set; }
    public string? CurrentProcess { get; set; }
    public string? PoolName { get; set; }
}

public class SessionDto
{
    public Guid SessionId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? DurationSeconds { get; set; }
}

public class QueueSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public int Pending { get; set; }
    public int Locked { get; set; }
    public int Completed { get; set; }
    public int Exceptioned { get; set; }
    public int Total { get; set; }
    public double HealthPercent { get; set; }
}

// ─── Process ─────────────────────────────────────────────────
public class ProcessDto
{
    public Guid ProcessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RecentExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDurationSeconds { get; set; }
}

public class ProcessRunRequestDto
{
    public string ProcessName { get; set; } = string.Empty;
    public string? TargetResource { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public int Priority { get; set; } = 5;
}

public class ProcessRunResponseDto
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}

// ─── Schedule ────────────────────────────────────────────────
public class ScheduleDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public string? TargetResource { get; set; }
    public int Priority { get; set; } = 5;
    public TimeSpan? SlaDeadline { get; set; }
    public bool AiOptimizationEnabled { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public bool BusinessHoursOnly { get; set; }
    public string? BusinessHoursStart { get; set; }
    public string? BusinessHoursEnd { get; set; }
    public string? StartupParametersXml { get; set; }
    public List<string>? DependsOn { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ScheduleOptimizationResultDto
{
    public Guid ScheduleId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string OriginalCron { get; set; } = string.Empty;
    public string? SuggestedCron { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string? EstimatedImpact { get; set; }
}

// ─── Alert ───────────────────────────────────────────────────
public class AlertRuleDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AlertRuleType RuleType { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public double? ThresholdValue { get; set; }
    public ComparisonOperator? Operator { get; set; }
    public string? MetricName { get; set; }
    public int? TimeWindowMinutes { get; set; }
    public int CooldownMinutes { get; set; } = 15;
    public List<NotificationChannel> Channels { get; set; } = new();
    public List<string>? EmailRecipients { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class AlertHistoryDto
{
    public Guid Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public double? TriggerValue { get; set; }
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime FiredAt { get; set; }
}

// ─── Analytics ───────────────────────────────────────────────
public class AnalyticsTrendDto
{
    public string ProcessName { get; set; } = string.Empty;
    public List<TrendDataPoint> DurationTrend { get; set; } = new();
    public List<TrendDataPoint> SuccessRateTrend { get; set; } = new();
    public List<TrendDataPoint> ThroughputTrend { get; set; } = new();
}

public class TrendDataPoint
{
    public DateTime Timestamp { get; set; }
    public double? Value { get; set; }
}

public class SlaComplianceDto
{
    public double OverallCompliancePercent { get; set; }
    public int TotalExecutions { get; set; }
    public int SlaBreaches { get; set; }
    public List<TrendDataPoint> ComplianceTrend { get; set; } = new();
    public List<SlaProcessDetail> WorstProcesses { get; set; } = new();
}

public class SlaProcessDetail
{
    public string ProcessName { get; set; } = string.Empty;
    public double CompliancePercent { get; set; }
    public int Breaches { get; set; }
}

public class ResourceHeatmapDto
{
    public string ResourceName { get; set; } = string.Empty;
    public List<HeatmapCell> Cells { get; set; } = new();
}

public class HeatmapCell
{
    public int Hour { get; set; }
    public int DayOfWeek { get; set; }
    public double UtilizationPercent { get; set; }
}

// ─── AI ──────────────────────────────────────────────────────
public class AIRecommendationDto
{
    public Guid Id { get; set; }
    public RecommendationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? EstimatedImpact { get; set; }
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public RecommendationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FailurePredictionDto
{
    public string ProcessName { get; set; } = string.Empty;
    public int Hour { get; set; }
    public double FailureProbability { get; set; }
}

public class CapacityForecastDto
{
    public DateTime Timestamp { get; set; }
    public double PredictedUtilization { get; set; }
    public double ActualUtilization { get; set; }
    public double PredictedDemand { get; set; }
}

// ─── Anomaly ─────────────────────────────────────────────────
public class AnomalyDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public double DeviationPercent { get; set; }
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public DateTime DetectedAt { get; set; }
}

// ─── Cutoff Policies ──────────────────────────────────────────
public class CutoffPolicyDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProcessName { get; set; }
    public int MaxRuntimeSeconds { get; set; } = 0;
    public TimeSpan? CutoffTime { get; set; }
    public int ActiveDays { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
    public bool ForceKill { get; set; } = false;
    public List<string> NotifyEmails { get; set; } = new();
}

public class CutoffEventDto
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string? ProcessName { get; set; }
    public string? ResourceName { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime TriggerTime { get; set; }
    public CutoffTriggerReason Reason { get; set; }
    public bool StopRequested { get; set; }
    public string? ResultMessage { get; set; }
}

public class CutoffCheckResultDto
{
    public int PoliciesEvaluated { get; set; }
    public int SessionsMatched { get; set; }
    public int SessionsStopped { get; set; }
    public List<CutoffSessionDetailDto> Details { get; set; } = new();
}

public class CutoffSessionDetailDto
{
    public Guid SessionId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? ResourceName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double RuntimeSeconds { get; set; }
    public bool Stopped { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─── Business Output Rules ───────────────────────────────────
public class BusinessOutputRuleDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string OutputType { get; set; } = "Excel";
    public string? FileNamePattern { get; set; }
    public string DeliveryMethod { get; set; } = "Email";
    public string DeliveryTarget { get; set; } = string.Empty;
    public bool DeliverOnFailure { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
}

public class BusinessOutputDeliveryLogDto
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public string? ProcessName { get; set; }
    public string? FileName { get; set; }
    public DateTime DeliveredAt { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? Message { get; set; }
}
