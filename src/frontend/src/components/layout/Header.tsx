import React, { useState, useEffect } from 'react';
import { Play, RefreshCw, Radio, Sparkles } from 'lucide-react';
import { ActiveTab } from './Sidebar';

interface HeaderProps {
  activeTab: ActiveTab;
  isLiveConnected: boolean;
  onRefresh: () => void;
  onOpenTriggerModal: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  activeTab,
  isLiveConnected,
  onRefresh,
  onOpenTriggerModal,
}) => {
  const [time, setTime] = useState<string>('');

  useEffect(() => {
    const update = () => {
      const now = new Date();
      setTime(now.toISOString().slice(11, 19) + ' UTC');
    };
    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, []);

  const titles: Record<ActiveTab, { title: string; subtitle: string }> = {
    dashboard: {
      title: 'Live Operations Center',
      subtitle: 'Real-time telemetry of runtime bots, active sessions, and work queues',
    },
    ai: {
      title: 'AI Insights & Predictive Engine',
      subtitle: 'ML-driven failure mitigation, capacity forecasting, and anomaly detection',
    },
    analytics: {
      title: 'Analytics & SLA Compliance',
      subtitle: 'Historical execution duration, throughput trends, and resource heatmaps',
    },
    schedules: {
      title: 'Smart Process Schedules',
      subtitle: 'Cron triggers, dependency chains, SLA deadlines, and AI optimization',
    },
    alerts: {
      title: 'Alerts & Incident Monitoring',
      subtitle: 'Multi-channel notification rules, operational anomalies, and audit log',
    },
    operations: {
      title: 'Cutoff Policies & Business Outputs',
      subtitle: 'Process termination controls and automated Excel delivery to Business users',
    },
  };

  const { title, subtitle } = titles[activeTab];

  return (
    <header
      style={{
        height: '70px',
        backgroundColor: 'var(--bg-header)',
        backdropFilter: 'var(--backdrop-blur)',
        WebkitBackdropFilter: 'var(--backdrop-blur)',
        borderBottom: '1px solid var(--border-subtle)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 28px',
        position: 'sticky',
        top: 0,
        zIndex: 10,
      }}
    >
      <div>
        <h1 style={{ fontSize: '18px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
          {title}
          {activeTab === 'ai' && <Sparkles size={16} color="var(--purple)" />}
        </h1>
        <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{subtitle}</p>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        {/* Live SignalR indicator */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '5px 12px',
            borderRadius: '9999px',
            background: isLiveConnected ? 'rgba(16, 185, 129, 0.1)' : 'rgba(244, 63, 94, 0.1)',
            border: `1px solid ${isLiveConnected ? 'rgba(16, 185, 129, 0.25)' : 'rgba(244, 63, 94, 0.25)'}`,
            fontSize: '12px',
            fontWeight: 500,
            color: isLiveConnected ? '#34d399' : '#fb7185',
          }}
        >
           <Radio size={12} />
           {isLiveConnected && <div className="pulse-dot online" style={{ width: '8px', height: '8px', marginTop: '10px' }} />}
           <span>{isLiveConnected ? 'SignalR Live' : 'Reconnecting...'}</span>
        </div>

        {/* UTC Clock */}
        <div
          className="mono"
          style={{
            fontSize: '12px',
            color: 'var(--text-secondary)',
            background: 'rgba(255, 255, 255, 0.04)',
            padding: '5px 10px',
            borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          {time}
        </div>

        {/* Refresh button */}
        <button
          onClick={onRefresh}
          className="btn-secondary"
          title="Manual Refresh"
          style={{ padding: '8px 12px' }}
        >
          <RefreshCw size={14} />
        </button>

        {/* Dispatch Run Process button */}
        <button
          onClick={onOpenTriggerModal}
          className="btn-primary"
        >
          <Play size={14} fill="currentColor" />
          <span>Dispatch Process</span>
        </button>
      </div>
    </header>
  );
};
