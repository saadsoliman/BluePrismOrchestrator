import {
  DashboardSummaryDto,
  ProcessDto,
  ScheduleDto,
  ScheduleOptimizationResultDto,
  AlertRuleDto,
  AlertHistoryDto,
  AnalyticsTrendDto,
  SlaComplianceDto,
  ResourceHeatmapDto,
  AIRecommendationDto,
  FailurePredictionDto,
  CapacityForecastDto,
  AnomalyDto,
  SessionDto,
  ResourceStatusDto,
  QueueSummaryDto,
  CutoffPolicyDto,
  CutoffEventDto,
  CutoffCheckResultDto,
  BusinessOutputRuleDto,
  BusinessOutputDeliveryLogDto,
  LLMConfigDto,
  LLMModelOptionDto,
} from '../types';

const API_BASE = '/api';

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`API ${res.status}: ${text || res.statusText}`);
  }

  return res.json();
}

export const api = {
  // Dashboard
  getDashboardSummary: () => fetchJson<DashboardSummaryDto>(`${API_BASE}/dashboard/summary`),

  // Processes
  getProcesses: () => fetchJson<ProcessDto[]>(`${API_BASE}/process`),
  runProcess: (name: string, targetResource?: string) =>
    fetchJson<{ executionId: string; status: string; message?: string }>(`${API_BASE}/process/${encodeURIComponent(name)}/run`, {
      method: 'POST',
      body: JSON.stringify({ processName: name, targetResource }),
    }),

  // Resources
  getResources: () => fetchJson<ResourceStatusDto[]>(`${API_BASE}/resource`),
  getResourceUtilization: (name: string, hours = 24) =>
    fetchJson<{ timestamp: string; value: number }[]>(`${API_BASE}/resource/${encodeURIComponent(name)}/utilization?hours=${hours}`),

  // Queues
  getQueues: () => fetchJson<QueueSummaryDto[]>(`${API_BASE}/queue`),

  // Schedules
  getSchedules: () => fetchJson<ScheduleDto[]>(`${API_BASE}/schedule`),
  createSchedule: (schedule: ScheduleDto) =>
    fetchJson<ScheduleDto>(`${API_BASE}/schedule`, {
      method: 'POST',
      body: JSON.stringify(schedule),
    }),
  updateSchedule: (id: string, schedule: ScheduleDto) =>
    fetchJson<ScheduleDto>(`${API_BASE}/schedule/${id}`, {
      method: 'PUT',
      body: JSON.stringify(schedule),
    }),
  deleteSchedule: async (id: string) => {
    const res = await fetch(`${API_BASE}/schedule/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error(`Failed to delete schedule ${id}`);
  },
  optimizeSchedules: () => fetchJson<ScheduleOptimizationResultDto[]>(`${API_BASE}/schedule/optimize`, { method: 'POST' }),

  // Alerts
  getAlertRules: () => fetchJson<AlertRuleDto[]>(`${API_BASE}/alert/rules`),
  createAlertRule: (rule: AlertRuleDto) =>
    fetchJson<AlertRuleDto>(`${API_BASE}/alert/rules`, {
      method: 'POST',
      body: JSON.stringify(rule),
    }),
  updateAlertRule: (id: string, rule: AlertRuleDto) =>
    fetchJson<AlertRuleDto>(`${API_BASE}/alert/rules/${id}`, {
      method: 'PUT',
      body: JSON.stringify(rule),
    }),
  deleteAlertRule: async (id: string) => {
    const res = await fetch(`${API_BASE}/alert/rules/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error(`Failed to delete rule ${id}`);
  },
  getAlertHistory: (limit = 50) => fetchJson<AlertHistoryDto[]>(`${API_BASE}/alert/history?limit=${limit}`),
  acknowledgeAlert: (id: string) => fetchJson<{ success: boolean }>(`${API_BASE}/alert/history/${id}/acknowledge`, { method: 'POST' }),

  // Analytics
  getTrends: (processName?: string, days = 7) =>
    fetchJson<AnalyticsTrendDto>(`${API_BASE}/analytics/trends?processName=${encodeURIComponent(processName || '')}&days=${days}`),
  getSlaCompliance: (days = 7) => fetchJson<SlaComplianceDto>(`${API_BASE}/analytics/sla-compliance?days=${days}`),
  getResourceHeatmap: () => fetchJson<ResourceHeatmapDto[]>(`${API_BASE}/analytics/resource-heatmap`),

  // AI
  getRecommendations: () => fetchJson<AIRecommendationDto[]>(`${API_BASE}/ai/recommendations`),
  acceptRecommendation: (id: string) => fetchJson<{ success: boolean }>(`${API_BASE}/ai/recommendations/${id}/accept`, { method: 'POST' }),
  dismissRecommendation: (id: string) => fetchJson<{ success: boolean }>(`${API_BASE}/ai/recommendations/${id}/dismiss`, { method: 'POST' }),
  getFailureHeatmap: () => fetchJson<FailurePredictionDto[]>(`${API_BASE}/ai/failure-heatmap`),
  getCapacityForecast: () => fetchJson<CapacityForecastDto[]>(`${API_BASE}/ai/capacity-forecast`),
  getAnomalies: (limit = 20) => fetchJson<AnomalyDto[]>(`${API_BASE}/ai/anomalies?limit=${limit}`),
  retrainAI: () => fetchJson<{ status: string }>(`${API_BASE}/ai/retrain`, { method: 'POST' }),

  // LLM Model Selection
  getLLMConfig: () => fetchJson<LLMConfigDto>(`${API_BASE}/llm/models`),
  setActiveModel: (modelId: string) =>
    fetchJson<{ success: boolean; activeModel: LLMModelOptionDto; message: string }>(
      `${API_BASE}/llm/model`,
      { method: 'POST', body: JSON.stringify({ modelId }) }
    ),

  // Sessions
  getSessions: (activeOnly = true) => fetchJson<SessionDto[]>(`${API_BASE}/session?activeOnly=${activeOnly}`),
  stopSession: (id: string, processName: string, resourceName: string) =>
    fetchJson<{ success: boolean; message: string }>(
      `${API_BASE}/session/${id}/stop?processName=${encodeURIComponent(processName)}&resourceName=${encodeURIComponent(resourceName)}`,
      { method: 'POST' }
    ),

  // Cutoff Policies
  getCutoffPolicies: () => fetchJson<CutoffPolicyDto[]>(`${API_BASE}/cutoff`),
  createCutoffPolicy: (policy: CutoffPolicyDto) =>
    fetchJson<CutoffPolicyDto>(`${API_BASE}/cutoff`, {
      method: 'POST',
      body: JSON.stringify(policy),
    }),
  updateCutoffPolicy: (id: string, policy: CutoffPolicyDto) =>
    fetchJson<CutoffPolicyDto>(`${API_BASE}/cutoff/${id}`, {
      method: 'PUT',
      body: JSON.stringify(policy),
    }),
  deleteCutoffPolicy: async (id: string) => {
    const res = await fetch(`${API_BASE}/cutoff/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error(`Failed to delete cutoff policy ${id}`);
  },
  toggleCutoffPolicy: (id: string, enabled: boolean) =>
    fetchJson<CutoffPolicyDto>(`${API_BASE}/cutoff/${id}/toggle?enabled=${enabled}`, {
      method: 'POST',
    }),
  evaluateCutoffs: () => fetchJson<CutoffCheckResultDto>(`${API_BASE}/cutoff/evaluate`, {
    method: 'POST',
  }),
  getCutoffHistory: (limit = 50) => fetchJson<CutoffEventDto[]>(`${API_BASE}/cutoff/history?limit=${limit}`),

  // Business Output Rules
  getBusinessOutputRules: () => fetchJson<BusinessOutputRuleDto[]>(`${API_BASE}/businessoutput`),
  createBusinessOutputRule: (rule: BusinessOutputRuleDto) =>
    fetchJson<BusinessOutputRuleDto>(`${API_BASE}/businessoutput`, {
      method: 'POST',
      body: JSON.stringify(rule),
    }),
  updateBusinessOutputRule: (id: string, rule: BusinessOutputRuleDto) =>
    fetchJson<BusinessOutputRuleDto>(`${API_BASE}/businessoutput/${id}`, {
      method: 'PUT',
      body: JSON.stringify(rule),
    }),
  deleteBusinessOutputRule: async (id: string) => {
    const res = await fetch(`${API_BASE}/businessoutput/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error(`Failed to delete output rule ${id}`);
  },
  toggleBusinessOutputRule: (id: string, enabled: boolean) =>
    fetchJson<BusinessOutputRuleDto>(`${API_BASE}/businessoutput/${id}/toggle?enabled=${enabled}`, {
      method: 'POST',
    }),
  deliverOutputs: () => fetchJson<BusinessOutputDeliveryLogDto[]>(`${API_BASE}/businessoutput/deliver`, {
    method: 'POST',
  }),
  getOutputDeliveryLogs: (limit = 50) => fetchJson<BusinessOutputDeliveryLogDto[]>(`${API_BASE}/businessoutput/logs?limit=${limit}`),
};
