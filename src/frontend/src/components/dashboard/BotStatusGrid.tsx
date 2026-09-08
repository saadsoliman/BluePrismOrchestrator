import React from 'react';
import { Bot, Server, Zap } from 'lucide-react';
import { ResourceStatusDto } from '../../types';

interface BotStatusGridProps {
  resources: ResourceStatusDto[];
}

export const BotStatusGrid: React.FC<BotStatusGridProps> = ({ resources }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Server size={16} color="var(--cyan)" />
            Blue Prism Runtime Bot Pool
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Live machine heartbeat & session occupancy
          </p>
        </div>
        <span
          style={{
            fontSize: '11px',
            color: 'var(--text-muted)',
            background: 'rgba(255, 255, 255, 0.04)',
            padding: '4px 10px',
            borderRadius: '9999px',
          }}
        >
          {resources.filter((r) => r.isOnline).length} / {resources.length} Online
        </span>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
          gap: '14px',
        }}
      >
        {resources.map((res) => {
          const isBusy = res.activeSessions > 0;
          return (
            <div
              key={res.name}
              style={{
                background: 'rgba(15, 23, 42, 0.6)',
                border: `1px solid ${
                  res.isOnline
                    ? isBusy
                      ? 'rgba(6, 182, 212, 0.35)'
                      : 'rgba(16, 185, 129, 0.3)'
                    : 'rgba(244, 63, 94, 0.25)'
                }`,
                borderRadius: 'var(--radius-sm)',
                padding: '14px',
                transition: 'all 0.2s ease',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '10px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <div
                    style={{
                      width: '28px',
                      height: '28px',
                      borderRadius: '6px',
                      background: res.isOnline ? 'rgba(59, 130, 246, 0.15)' : 'rgba(255, 255, 255, 0.04)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Bot size={15} color={res.isOnline ? 'var(--primary-light)' : 'var(--text-muted)'} />
                  </div>
                  <div>
                    <div style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>{res.name}</div>
                    <div style={{ fontSize: '10px', color: 'var(--text-muted)' }}>{res.poolName || 'Default Pool'}</div>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <div className={`pulse-dot ${res.isOnline ? (isBusy ? 'running' : 'online') : 'offline'}`} />
                  <span
                    style={{
                      fontSize: '10px',
                      fontWeight: 700,
                      color: res.isOnline ? (isBusy ? 'var(--cyan)' : 'var(--emerald)') : 'var(--rose)',
                    }}
                  >
                    {res.isOnline ? (isBusy ? 'BUSY' : 'IDLE') : 'OFFLINE'}
                  </span>
                </div>
              </div>

              {/* Workload metric */}
              <div style={{ marginTop: '12px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px', marginBottom: '4px' }}>
                  <span style={{ color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                    <Zap size={10} /> Active Sessions
                  </span>
                  <span className="mono" style={{ fontWeight: 600, color: '#ffffff' }}>
                    {res.activeSessions}
                  </span>
                </div>

                <div className="progress-track">
                  <div
                    className={`progress-fill ${
                      !res.isOnline ? 'danger' : isBusy ? 'primary' : 'success'
                    }`}
                    style={{ width: `${res.isOnline ? res.utilizationPercent : 0}%` }}
                  />
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
