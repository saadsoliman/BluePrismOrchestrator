// TypeScript types for Blue Prism Smart Orchestrator

export type ExecutionStatus = 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Terminated' | 'Stopped';
export type AlertSeverity = 'Info' | 'Warning' | 'Critical';
export type AlertRuleType = 'Threshold' | 'Anomaly' | 'SlaBreach' | 'ResourceDown' | 'ConsecutiveFailures';
export type ComparisonOperator = 'GreaterThan' | 'LessThan' | 'GreaterThanOrEqual' | 'LessThanOrEqual' | 'Equal' | 'NotEqual';
export type NotificationChannel = 'InApp' | 'Email' | 'Teams' | 'Webhook';
export type RecommendationType = 'ResourceReassignment' | 'ScheduleOptimization' | 'CapacityScaling' | 'ProcessConfiguration' | 'QueueManagement' | 'FailurePrevention';
export type RecommendationStatus = 'Active' | 'Accepted' | 'Dismissed' | 'Expired' | 'Implemented';

export interface ResourceStatusDto {
  name: string;
  isOnline: boolean;
  activeSessions: number;
  utilizationPercent: number;
  currentProcess?: string;
  poolName?: string;
}

export interface SessionDto {
  sessionId: string;
  processName: string;
  resourceName: string;
  status: string;
  startTime?: string;
  endTime?: string;
  durationSeconds?: number;
}

export interface QueueSummaryDto {
  name: string;
  pending: number;
  locked: number;
  completed: number;
  exceptioned: number;
  total: number;
  healthPercent: number;
}

export interface AlertHistoryDto {
  id: string;
  ruleName: string;
  severity: AlertSeverity;
  message: string;
  triggerValue?: number;
  processName?: string;
  resourceName?: string;
  isAcknowledged: boolean;
  firedAt: string;
}

export interface DashboardSummaryDto {
  totalResources: number;
  onlineResources: number;
  activeSessions: number;
  totalProcesses: number;
  successRate24h: number;
  avgQueueWaitSeconds: number;
  pendingQueueItems: number;
  totalExecutionsToday: number;
  resources: ResourceStatusDto[];
  activeSessionsList: SessionDto[];
  queues: QueueSummaryDto[];
  recentAlerts: AlertHistoryDto[];
}

export interface ProcessDto {
  processId: string;
  name: string;
  description?: string;
  recentExecutions: number;
  successRate: number;
  avgDurationSeconds: number;
}

export interface ScheduleDto {
  id?: string;
  name: string;
  processName: string;
  cronExpression?: string;
  targetResource?: string;
  priority: number;
  slaDeadline?: string;
  aiOptimizationEnabled: boolean;
  maxRetries: number;
  businessHoursOnly: boolean;
  businessHoursStart?: string;
  businessHoursEnd?: string;
  startupParametersXml?: string;
  dependsOn?: string[];
  isEnabled: boolean;
}

export interface ScheduleOptimizationResultDto {
  scheduleId: number;
  processName: string;
  originalCron: string;
  suggestedCron: string;
  reasoning: string;
  confidenceScore: number;
  estimatedImpact: string;
}

export interface AlertRuleDto {
  id?: string;
  name: string;
  description?: string;
  ruleType: AlertRuleType;
  severity: AlertSeverity;
  processName?: string;
  resourceName?: string;
  thresholdValue?: number;
  operator?: ComparisonOperator;
  metricName?: string;
  timeWindowMinutes?: number;
  cooldownMinutes: number;
  channels: NotificationChannel[];
  emailRecipients?: string[];
  isEnabled: boolean;
}

export interface TrendDataPoint {
  timestamp: string;
  value?: number | null;
}

export interface AnalyticsTrendDto {
  processName: string;
  durationTrend: TrendDataPoint[];
  successRateTrend: TrendDataPoint[];
  throughputTrend: TrendDataPoint[];
}

export interface SlaProcessDetail {
  processName: string;
  compliancePercent: number;
  breaches: number;
}

export interface SlaComplianceDto {
  overallCompliancePercent: number;
  totalExecutions: number;
  slaBreaches: number;
  complianceTrend: TrendDataPoint[];
  worstProcesses: SlaProcessDetail[];
}

export interface HeatmapCell {
  hour: number;
  dayOfWeek: number;
  utilizationPercent: number;
}

export interface ResourceHeatmapDto {
  resourceName: string;
  cells: HeatmapCell[];
}

export interface AIRecommendationDto {
  id: string;
  type: RecommendationType;
  title: string;
  description: string;
  confidence: number;
  estimatedImpact?: string;
  processName?: string;
  resourceName?: string;
  status: RecommendationStatus;
  createdAt: string;
}

export interface FailurePredictionDto {
  processName: string;
  hour: number;
  failureProbability: number;
}

export interface CapacityForecastDto {
  timestamp: string;
  predictedUtilization: number;
  actualUtilization: number;
  predictedDemand: number;
}

export interface AnomalyDto {
  id: string;
  type: string;
  description: string;
  severity: AlertSeverity;
  deviationPercent: number;
  processName?: string;
  resourceName?: string;
  detectedAt: string;
}

// ─── LLM Model Selection ─────────────────────────────────────
export interface LLMModelOptionDto {
  id: string;
  name: string;
  model?: string;
  url?: string;
  provider: string;
  isCustom: boolean;
  isLocal: boolean;
  maxTokens?: number;
}

export interface LLMConfigDto {
  currentModel: string;
  currentProvider: string;
  baseUrl: string;
  maxTokens: number;
  temperature: number;
  models: LLMModelOptionDto[];
}

// ─── Cutoff Policies ──────────────────────────────────────────
export interface CutoffPolicyDto {
  id?: string;
  name: string;
  description?: string;
  processName?: string;
  maxRuntimeSeconds: number;
  cutoffTime?: string | null;
  activeDays: number;
  isEnabled: boolean;
  forceKill: boolean;
  notifyEmails?: string[];
}

export interface CutoffEventDto {
  id: string;
  policyId: string;
  processName?: string;
  resourceName?: string;
  sessionStartTime?: string;
  triggerTime: string;
  reason: 'DurationExceeded' | 'CutoffTimeReached';
  stopRequested: boolean;
  resultMessage?: string;
}

export interface CutoffSessionDetailDto {
  sessionId: string;
  processName: string;
  resourceName?: string;
  reason: string;
  runtimeSeconds: number;
  stopped: boolean;
  message: string;
}

export interface CutoffCheckResultDto {
  policiesEvaluated: number;
  sessionsMatched: number;
  sessionsStopped: number;
  details: CutoffSessionDetailDto[];
}

// ─── Business Output Rules ───────────────────────────────────
export interface BusinessOutputRuleDto {
  id?: string;
  name: string;
  description?: string;
  processName: string;
  outputType: string;
  fileNamePattern?: string;
  deliveryMethod: string;
  deliveryTarget: string;
  deliverOnFailure: boolean;
  isEnabled: boolean;
}

export interface BusinessOutputDeliveryLogDto {
  id: string;
  ruleId: string;
  processName?: string;
  fileName?: string;
  deliveredAt: string;
  status: 'Pending' | 'Delivered' | 'Skipped' | 'Failed';
  message?: string;
}
