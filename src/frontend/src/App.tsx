import React, { useState, useEffect, useCallback } from 'react';
import { Sidebar, ActiveTab } from './components/layout/Sidebar';
import { Header } from './components/layout/Header';
import { DashboardPage } from './pages/DashboardPage';
import { AIInsightsPage } from './pages/AIInsightsPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { SchedulesPage } from './pages/SchedulesPage';
import { AlertsPage } from './pages/AlertsPage';
import { OperationsPage } from './pages/OperationsPage';
import { DispatchProcessModal } from './components/dashboard/DispatchProcessModal';
import { useSignalR } from './hooks/useSignalR';
import { api } from './api/client';
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
  BusinessOutputRuleDto,
  BusinessOutputDeliveryLogDto,
} from './types';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard');
  const [isTriggerModalOpen, setIsTriggerModalOpen] = useState(false);

  // Core telemetry state
  const [summary, setSummary] = useState<DashboardSummaryDto | null>(null);
  const [processes, setProcesses] = useState<ProcessDto[]>([]);
  const [schedules, setSchedules] = useState<ScheduleDto[]>([]);
  const [optimizations, setOptimizations] = useState<ScheduleOptimizationResultDto[]>([]);
  const [alertRules, setAlertRules] = useState<AlertRuleDto[]>([]);
  const [alertHistory, setAlertHistory] = useState<AlertHistoryDto[]>([]);

  // Operations state (cutoff policies + business outputs)
  const [cutoffPolicies, setCutoffPolicies] = useState<CutoffPolicyDto[]>([]);
  const [cutoffHistory, setCutoffHistory] = useState<CutoffEventDto[]>([]);
  const [outputRules, setOutputRules] = useState<BusinessOutputRuleDto[]>([]);
  const [outputLogs, setOutputLogs] = useState<BusinessOutputDeliveryLogDto[]>([]);

  // AI & Analytics state
  const [recommendations, setRecommendations] = useState<AIRecommendationDto[]>([]);
  const [failureHeatmap, setFailureHeatmap] = useState<FailurePredictionDto[]>([]);
  const [capacityForecast, setCapacityForecast] = useState<CapacityForecastDto[]>([]);
  const [anomalies, setAnomalies] = useState<AnomalyDto[]>([]);
  const [trends, setTrends] = useState<AnalyticsTrendDto | null>(null);
  const [sla, setSla] = useState<SlaComplianceDto | null>(null);
  const [resourceHeatmap, setResourceHeatmap] = useState<ResourceHeatmapDto[]>([]);
  const [selectedTrendProcess, setSelectedTrendProcess] = useState<string>('Invoice_Processing');

  const { isConnected, on } = useSignalR();

  // Load all initial data
  const loadInitialData = useCallback(async () => {
    try {
      const [
        sumData,
        procData,
        schedData,
        rulesData,
        histData,
        recsData,
        heatData,
        foreData,
        anomData,
        slaData,
        rHeatData,
        cutoffPoliciesData,
        cutoffHistoryData,
        outputRulesData,
        outputLogsData,
      ] = await Promise.all([
        api.getDashboardSummary(),
        api.getProcesses(),
        api.getSchedules(),
        api.getAlertRules(),
        api.getAlertHistory(30),
        api.getRecommendations(),
        api.getFailureHeatmap(),
        api.getCapacityForecast(),
        api.getAnomalies(15),
        api.getSlaCompliance(7),
        api.getResourceHeatmap(),
        api.getCutoffPolicies(),
        api.getCutoffHistory(50),
        api.getBusinessOutputRules(),
        api.getOutputDeliveryLogs(50),
      ]);

      setSummary(sumData);
      setProcesses(procData);
      setSchedules(schedData);
      setAlertRules(rulesData);
      setAlertHistory(histData);
      setRecommendations(recsData);
      setFailureHeatmap(heatData);
      setCapacityForecast(foreData);
      setAnomalies(anomData);
      setSla(slaData);
      setResourceHeatmap(rHeatData);
      setCutoffPolicies(cutoffPoliciesData);
      setCutoffHistory(cutoffHistoryData);
      setOutputRules(outputRulesData);
      setOutputLogs(outputLogsData);
    } catch (err) {
      console.error('Failed to load initial data:', err);
    }
  }, []);

  // Load trends when process or tab changes
  const loadTrends = useCallback(async (processName: string) => {
    try {
      const trendData = await api.getTrends(processName, 7);
      setTrends(trendData);
    } catch (err) {
      console.error('Failed to load trends:', err);
    }
  }, []);

  useEffect(() => {
    loadInitialData();
  }, [loadInitialData]);

  useEffect(() => {
    if (selectedTrendProcess) {
      loadTrends(selectedTrendProcess);
    }
  }, [selectedTrendProcess, loadTrends]);

  // Setup Real-Time SignalR Event Listeners
  useEffect(() => {
    const unsubSessions = on('SessionsUpdated', (updatedSessions: SessionDto[]) => {
      setSummary((prev) => {
        if (!prev) return null;
        const merged = [...prev.activeSessionsList];
        updatedSessions.forEach((incoming) => {
          const idx = merged.findIndex((s) => s.sessionId === incoming.sessionId);
          if (idx >= 0) merged[idx] = incoming;
          else merged.unshift(incoming);
        });
        return {
          ...prev,
          activeSessions: merged.length,
          activeSessionsList: merged,
        };
      });
    });

    const unsubResources = on('ResourcesUpdated', (updatedResources: ResourceStatusDto[]) => {
      setSummary((prev) => {
        if (!prev) return null;
        return {
          ...prev,
          onlineResources: updatedResources.filter((r) => r.isOnline).length,
          resources: updatedResources,
        };
      });
    });

    const unsubQueues = on('QueuesUpdated', (updatedQueues: QueueSummaryDto[]) => {
      setSummary((prev) => {
        if (!prev) return null;
        return {
          ...prev,
          pendingQueueItems: updatedQueues.reduce((acc, q) => acc + q.pending, 0),
          queues: updatedQueues,
        };
      });
    });

    const unsubAlert = on('AlertFired', (newAlert: any) => {
      const mapped: AlertHistoryDto = {
        id: newAlert.id,
        ruleName: newAlert.ruleName,
        severity: newAlert.severity,
        message: newAlert.message,
        processName: newAlert.processName,
        resourceName: newAlert.resourceName,
        isAcknowledged: false,
        firedAt: newAlert.firedAt,
      };
      setAlertHistory((prev) => [mapped, ...prev]);
      setSummary((prev) => (prev ? { ...prev, recentAlerts: [mapped, ...prev.recentAlerts] } : null));
    });

    const unsubRecs = on('RecommendationsUpdated', () => {
      api.getRecommendations().then(setRecommendations);
    });

    const unsubAnomalies = on('AnomaliesUpdated', (newAnomalies: AnomalyDto[]) => {
      setAnomalies(newAnomalies);
    });

    return () => {
      unsubSessions();
      unsubResources();
      unsubQueues();
      unsubAlert();
      unsubRecs();
      unsubAnomalies();
    };
  }, [on]);

  // Handler functions
  const handleStopSession = async (id: string, processName: string, resourceName: string) => {
    try {
      await api.stopSession(id, processName, resourceName);
      loadInitialData();
    } catch (err) {
      console.error('Stop session failed:', err);
    }
  };

  const handleAcknowledgeAlert = async (id: string) => {
    try {
      await api.acknowledgeAlert(id);
      setAlertHistory((prev) =>
        prev.map((a) => (a.id === id ? { ...a, isAcknowledged: true } : a))
      );
      setSummary((prev) =>
        prev
          ? {
              ...prev,
              recentAlerts: prev.recentAlerts.map((a) =>
                a.id === id ? { ...a, isAcknowledged: true } : a
              ),
            }
          : null
      );
    } catch (err) {
      console.error('Acknowledge alert failed:', err);
    }
  };

  const handleAcceptRecommendation = async (id: string) => {
    try {
      await api.acceptRecommendation(id);
      setRecommendations((prev) => prev.filter((r) => r.id !== id));
    } catch (err) {
      console.error('Accept recommendation failed:', err);
    }
  };

  const handleDismissRecommendation = async (id: string) => {
    try {
      await api.dismissRecommendation(id);
      setRecommendations((prev) => prev.filter((r) => r.id !== id));
    } catch (err) {
      console.error('Dismiss recommendation failed:', err);
    }
  };

  const handleRetrainAI = async () => {
    await api.retrainAI();
    const [recs, heat, fore] = await Promise.all([
      api.getRecommendations(),
      api.getFailureHeatmap(),
      api.getCapacityForecast(),
    ]);
    setRecommendations(recs);
    setFailureHeatmap(heat);
    setCapacityForecast(fore);
  };

  const handleSaveSchedule = async (schedule: ScheduleDto) => {
    try {
      const created = await api.createSchedule(schedule);
      setSchedules((prev) => [...prev, created]);
    } catch (err) {
      console.error('Create schedule failed:', err);
    }
  };

  const handleToggleSchedule = async (schedule: ScheduleDto) => {
    if (!schedule.id) return;
    try {
      const updated = await api.updateSchedule(schedule.id, schedule);
      setSchedules((prev) => prev.map((s) => (s.id === schedule.id ? updated : s)));
    } catch (err) {
      console.error('Update schedule failed:', err);
    }
  };

  const handleDeleteSchedule = async (id: string) => {
    try {
      await api.deleteSchedule(id);
      setSchedules((prev) => prev.filter((s) => s.id !== id));
    } catch (err) {
      console.error('Delete schedule failed:', err);
    }
  };

  const handleOptimizeSchedules = async () => {
    try {
      const opts = await api.optimizeSchedules();
      setOptimizations(opts);
    } catch (err) {
      console.error('Schedule optimization failed:', err);
    }
  };

  const handleSaveRule = async (rule: AlertRuleDto) => {
    try {
      const created = await api.createAlertRule(rule);
      setAlertRules((prev) => [...prev, created]);
    } catch (err) {
      console.error('Save rule failed:', err);
    }
  };

  const handleDeleteRule = async (id: string) => {
    try {
      await api.deleteAlertRule(id);
      setAlertRules((prev) => prev.filter((r) => r.id !== id));
    } catch (err) {
      console.error('Delete rule failed:', err);
    }
  };

  const handleToggleRule = async (rule: AlertRuleDto) => {
    if (!rule.id) return;
    try {
      const updated = await api.updateAlertRule(rule.id, rule);
      setAlertRules((prev) => prev.map((r) => (r.id === rule.id ? updated : r)));
    } catch (err) {
      console.error('Update rule failed:', err);
    }
  };

  // ─── Cutoff Policy Handlers ────────────────────────────────
  const handleSaveCutoffPolicy = async (policy: CutoffPolicyDto) => {
    try {
      const created = await api.createCutoffPolicy(policy);
      setCutoffPolicies((prev) => [...prev, created]);
    } catch (err) {
      console.error('Save cutoff policy failed:', err);
    }
  };

  const handleUpdateCutoffPolicy = async (id: string, policy: CutoffPolicyDto) => {
    try {
      const updated = await api.updateCutoffPolicy(id, policy);
      setCutoffPolicies((prev) => prev.map((p) => (p.id === id ? updated : p)));
    } catch (err) {
      console.error('Update cutoff policy failed:', err);
    }
  };

  const handleDeleteCutoffPolicy = async (id: string) => {
    try {
      await api.deleteCutoffPolicy(id);
      setCutoffPolicies((prev) => prev.filter((p) => p.id !== id));
    } catch (err) {
      console.error('Delete cutoff policy failed:', err);
    }
  };

  const handleToggleCutoffPolicy = async (id: string, enabled: boolean) => {
    try {
      const updated = await api.toggleCutoffPolicy(id, enabled);
      setCutoffPolicies((prev) => prev.map((p) => (p.id === id ? updated : p)));
    } catch (err) {
      console.error('Toggle cutoff policy failed:', err);
    }
  };

  const handleEvaluateCutoffs = async () => {
    try {
      await api.evaluateCutoffs();
      const [history] = await Promise.all([api.getCutoffHistory(50)]);
      setCutoffHistory(history);
    } catch (err) {
      console.error('Evaluate cutoffs failed:', err);
    }
  };

  // ─── Business Output Handlers ────────────────────────────────
  const handleSaveOutputRule = async (rule: BusinessOutputRuleDto) => {
    try {
      const created = await api.createBusinessOutputRule(rule);
      setOutputRules((prev) => [...prev, created]);
    } catch (err) {
      console.error('Save output rule failed:', err);
    }
  };

  const handleUpdateOutputRule = async (id: string, rule: BusinessOutputRuleDto) => {
    try {
      const updated = await api.updateBusinessOutputRule(id, rule);
      setOutputRules((prev) => prev.map((r) => (r.id === id ? updated : r)));
    } catch (err) {
      console.error('Update output rule failed:', err);
    }
  };

  const handleDeleteOutputRule = async (id: string) => {
    try {
      await api.deleteBusinessOutputRule(id);
      setOutputRules((prev) => prev.filter((r) => r.id !== id));
    } catch (err) {
      console.error('Delete output rule failed:', err);
    }
  };

  const handleToggleOutputRule = async (id: string, enabled: boolean) => {
    try {
      const updated = await api.toggleBusinessOutputRule(id, enabled);
      setOutputRules((prev) => prev.map((r) => (r.id === id ? updated : r)));
    } catch (err) {
      console.error('Toggle output rule failed:', err);
    }
  };

  const handleDeliverOutputs = async () => {
    try {
      await api.deliverOutputs();
      const [logs] = await Promise.all([api.getOutputDeliveryLogs(50)]);
      setOutputLogs(logs);
    } catch (err) {
      console.error('Deliver outputs failed:', err);
    }
  };

  const processNames = processes.map((p) => p.name);
  const resourceNames = summary?.resources.map((r) => r.name) || [];

  return (
    <div style={{ display: 'flex', width: '100%', minHeight: '100vh', backgroundColor: 'var(--bg-main)' }}>
      {/* Sidebar Navigation */}
      <Sidebar
        activeTab={activeTab}
        onTabChange={setActiveTab}
        activeAlertCount={alertHistory.filter((a) => !a.isAcknowledged).length}
        activeRecommendationCount={recommendations.length}
      />

      {/* Main Content Area */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <Header
          activeTab={activeTab}
          isLiveConnected={isConnected}
          onRefresh={loadInitialData}
          onOpenTriggerModal={() => setIsTriggerModalOpen(true)}
        />

        <main style={{ flex: 1, padding: '28px', overflowY: 'auto' }}>
          {activeTab === 'dashboard' && (
            <DashboardPage
              summary={summary}
              onStopSession={handleStopSession}
              onAcknowledgeAlert={handleAcknowledgeAlert}
            />
          )}

          {activeTab === 'ai' && (
            <AIInsightsPage
              recommendations={recommendations}
              heatmap={failureHeatmap}
              forecast={capacityForecast}
              anomalies={anomalies}
              onAcceptRecommendation={handleAcceptRecommendation}
              onDismissRecommendation={handleDismissRecommendation}
              onRetrain={handleRetrainAI}
            />
          )}

          {activeTab === 'analytics' && (
            <AnalyticsPage
              trends={trends}
              sla={sla}
              heatmap={resourceHeatmap}
              processes={processNames}
              selectedProcess={selectedTrendProcess}
              onSelectProcess={setSelectedTrendProcess}
            />
          )}

          {activeTab === 'schedules' && (
            <SchedulesPage
              schedules={schedules}
              onToggleSchedule={handleToggleSchedule}
              onDeleteSchedule={handleDeleteSchedule}
              onSaveSchedule={handleSaveSchedule}
              onOptimize={handleOptimizeSchedules}
              optimizations={optimizations}
              processes={processNames}
              resources={resourceNames}
            />
          )}

          {activeTab === 'alerts' && (
            <AlertsPage
              rules={alertRules}
              history={alertHistory}
              onSaveRule={handleSaveRule}
              onDeleteRule={handleDeleteRule}
              onToggleRule={handleToggleRule}
              onAcknowledgeAlert={handleAcknowledgeAlert}
              processes={processNames}
            />
          )}

          {activeTab === 'operations' && (
            <OperationsPage
              cutoffPolicies={cutoffPolicies}
              cutoffHistory={cutoffHistory}
              outputRules={outputRules}
              outputLogs={outputLogs}
              processes={processNames}
              onSaveCutoffPolicy={handleSaveCutoffPolicy}
              onUpdateCutoffPolicy={handleUpdateCutoffPolicy}
              onDeleteCutoffPolicy={handleDeleteCutoffPolicy}
              onToggleCutoffPolicy={handleToggleCutoffPolicy}
              onEvaluateCutoffs={handleEvaluateCutoffs}
              onSaveOutputRule={handleSaveOutputRule}
              onUpdateOutputRule={handleUpdateOutputRule}
              onDeleteOutputRule={handleDeleteOutputRule}
              onToggleOutputRule={handleToggleOutputRule}
              onDeliverOutputs={handleDeliverOutputs}
            />
          )}
        </main>
      </div>

      {/* Quick Trigger Modal */}
      <DispatchProcessModal
        isOpen={isTriggerModalOpen}
        onClose={() => setIsTriggerModalOpen(false)}
        processes={processes}
        resources={summary?.resources || []}
        onDispatched={loadInitialData}
      />
    </div>
  );
};
