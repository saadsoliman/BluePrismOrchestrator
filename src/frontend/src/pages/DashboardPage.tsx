import React from 'react';
import { KPICards } from '../components/dashboard/KPICards';
import { BotStatusGrid } from '../components/dashboard/BotStatusGrid';
import { ExecutionTimeline } from '../components/dashboard/ExecutionTimeline';
import { QueueDepthGauge } from '../components/dashboard/QueueDepthGauge';
import { AlertsTicker } from '../components/dashboard/AlertsTicker';
import { DashboardSummaryDto } from '../types';

interface DashboardPageProps {
  summary: DashboardSummaryDto | null;
  onStopSession: (id: string, process: string, resource: string) => void;
  onAcknowledgeAlert: (id: string) => void;
}

export const DashboardPage: React.FC<DashboardPageProps> = ({
  summary,
  onStopSession,
  onAcknowledgeAlert,
}) => {
  if (!summary) {
    return (
      <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>
        Loading real-time orchestration telemetry...
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {/* 1. Executive KPI Cards */}
      <KPICards summary={summary} />

      {/* 2. Main Live Grid (Bots & Queue Gauges) */}
      <div className="grid-two-col">
        <BotStatusGrid resources={summary.resources} />
        <QueueDepthGauge queues={summary.queues} />
      </div>

      {/* 3. Operational Streams (Sessions & Alerts) */}
      <div className="grid-two-col">
        <ExecutionTimeline
          sessions={summary.activeSessionsList}
          onStopSession={onStopSession}
        />
        <AlertsTicker
          alerts={summary.recentAlerts}
          onAcknowledge={onAcknowledgeAlert}
        />
      </div>
    </div>
  );
};
