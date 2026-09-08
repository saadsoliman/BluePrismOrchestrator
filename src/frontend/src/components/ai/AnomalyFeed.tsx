import React from 'react';
import { AlertCircle, Zap, Clock, ShieldAlert } from 'lucide-react';
import { AnomalyDto } from '../../types';

interface AnomalyFeedProps {
  anomalies: AnomalyDto[];
}

export const AnomalyFeed: React.FC<AnomalyFeedProps> = ({ anomalies }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ShieldAlert size={16} color="var(--rose)" />
            AI Anomaly Detection Feed
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Dynamic statistical deviation detection (IQR / 2.5σ threshold)
          </p>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {anomalies.map((a) => {
          const isCritical = a.severity === 'Critical';
          return (
            <div
              key={a.id}
              style={{
                padding: '14px',
                background: isCritical ? 'rgba(244, 63, 94, 0.08)' : 'rgba(245, 158, 11, 0.08)',
                border: `1px solid ${isCritical ? 'rgba(244, 63, 94, 0.3)' : 'rgba(245, 158, 11, 0.3)'}`,
                borderRadius: 'var(--radius-sm)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <div
                  style={{
                    width: '32px',
                    height: '32px',
                    borderRadius: '8px',
                    background: isCritical ? 'rgba(244, 63, 94, 0.2)' : 'rgba(245, 158, 11, 0.2)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  {isCritical ? <AlertCircle size={16} color="var(--rose)" /> : <Zap size={16} color="var(--amber)" />}
                </div>

                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>
                      {a.type}
                    </span>
                    <span
                      className="mono"
                      style={{
                        fontSize: '11px',
                        padding: '2px 6px',
                        borderRadius: '4px',
                        background: isCritical ? 'rgba(244, 63, 94, 0.2)' : 'rgba(245, 158, 11, 0.2)',
                        color: isCritical ? '#fb7185' : '#fbbf24',
                        fontWeight: 700,
                      }}
                    >
                      +{a.deviationPercent.toFixed(1)}% vs Baseline
                    </span>
                  </div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {a.description}
                  </div>
                </div>
              </div>

              <div style={{ textAlign: 'right' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '11px', color: 'var(--text-muted)' }}>
                  <Clock size={11} />
                  <span>{new Date(a.detectedAt).toLocaleTimeString()}</span>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
