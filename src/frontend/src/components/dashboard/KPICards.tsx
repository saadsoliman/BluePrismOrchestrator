import React from 'react';
import { Bot, Activity, CheckCircle, Clock, Layers, Flame } from 'lucide-react';
import { DashboardSummaryDto } from '../../types';

interface KPICardsProps {
  summary: DashboardSummaryDto;
}

export const KPICards: React.FC<KPICardsProps> = ({ summary }) => {
  const cards = [
    {
      title: 'Active Runtime Bots',
      value: `${summary.onlineResources} / ${summary.totalResources}`,
      subtitle: `${((summary.onlineResources / Math.max(summary.totalResources, 1)) * 100).toFixed(0)}% available in pools`,
      icon: Bot,
      color: 'var(--emerald)',
      glow: 'var(--emerald-glow)',
    },
    {
      title: 'Active Sessions',
      value: summary.activeSessions,
      subtitle: 'Currently executing tasks',
      icon: Activity,
      color: 'var(--cyan)',
      glow: 'var(--cyan-glow)',
    },
    {
      title: '24h Success Rate',
      value: `${summary.successRate24h}%`,
      subtitle: 'Across all enterprise queues',
      icon: CheckCircle,
      color: 'var(--primary)',
      glow: 'var(--primary-glow)',
    },
    {
      title: 'Work Queue Backlog',
      value: summary.pendingQueueItems.toLocaleString(),
      subtitle: `Avg latency: ${summary.avgQueueWaitSeconds.toFixed(1)}s`,
      icon: Layers,
      color: summary.pendingQueueItems > 150 ? 'var(--amber)' : 'var(--purple)',
      glow: summary.pendingQueueItems > 150 ? 'var(--amber-glow)' : 'var(--purple-glow)',
    },
    {
      title: 'Executions Today',
      value: summary.totalExecutionsToday,
      subtitle: 'Scheduled + AI-triggered',
      icon: Flame,
      color: '#f97316',
      glow: 'rgba(249, 115, 22, 0.35)',
    },
    {
      title: 'Total Processes',
      value: summary.totalProcesses,
      subtitle: 'Catalogued Blue Prism workflows',
      icon: Clock,
      color: 'var(--text-highlight)',
      glow: 'rgba(255, 255, 255, 0.1)',
    },
  ];

  return (
    <div className="grid-kpi">
      {cards.map((card, idx) => {
        const Icon = card.icon;
        return (
          <div
            key={idx}
            className="glass-panel"
            style={{
              padding: '20px',
              position: 'relative',
              overflow: 'hidden',
            }}
          >
            {/* Ambient background glow orb */}
            <div
              style={{
                position: 'absolute',
                top: '-20px',
                right: '-20px',
                width: '70px',
                height: '70px',
                borderRadius: '50%',
                background: card.glow,
                filter: 'blur(28px)',
                pointerEvents: 'none',
              }}
            />

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
              <span style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)' }}>
                {card.title}
              </span>
              <div
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: '8px',
                  background: 'rgba(255, 255, 255, 0.05)',
                  border: '1px solid var(--border-subtle)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <Icon size={16} color={card.color} />
              </div>
            </div>

            <div
              className="mono"
              style={{
                fontSize: '26px',
                fontWeight: 700,
                color: '#ffffff',
                lineHeight: 1.2,
                marginBottom: '4px',
              }}
            >
              {card.value}
            </div>

            <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
              {card.subtitle}
            </div>
          </div>
        );
      })}
    </div>
  );
};
