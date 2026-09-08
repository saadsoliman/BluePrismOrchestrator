import React from 'react';
import { Bell, AlertTriangle, AlertCircle, Info, Check } from 'lucide-react';
import { AlertHistoryDto } from '../../types';

interface AlertsTickerProps {
  alerts: AlertHistoryDto[];
  onAcknowledge?: (alertId: string) => void;
}

export const AlertsTicker: React.FC<AlertsTickerProps> = ({ alerts, onAcknowledge }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Bell size={16} color="var(--rose)" />
            Operational Alerts Stream
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Threshold breaches, resource drops, and pattern detections
          </p>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {alerts.length === 0 ? (
          <div style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)', fontSize: '13px' }}>
            All systems nominal. No active operational alerts.
          </div>
        ) : (
          alerts.slice(0, 5).map((a) => {
            const isCritical = a.severity === 'Critical';
            const isWarning = a.severity === 'Warning';

            return (
              <div
                key={a.id}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '12px 14px',
                  background: isCritical
                    ? 'rgba(244, 63, 94, 0.08)'
                    : isWarning
                    ? 'rgba(245, 158, 11, 0.08)'
                    : 'rgba(59, 130, 246, 0.08)',
                  border: `1px solid ${
                    isCritical
                      ? 'rgba(244, 63, 94, 0.3)'
                      : isWarning
                      ? 'rgba(245, 158, 11, 0.3)'
                      : 'rgba(59, 130, 246, 0.3)'
                  }`,
                  borderRadius: 'var(--radius-sm)',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  {isCritical && <AlertCircle size={18} color="var(--rose)" />}
                  {isWarning && <AlertTriangle size={18} color="var(--amber)" />}
                  {!isCritical && !isWarning && <Info size={18} color="var(--primary)" />}

                  <div>
                    <div style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>
                      {a.ruleName}
                    </div>
                    <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                      {a.message}
                    </div>
                    <div style={{ fontSize: '10px', color: 'var(--text-muted)', marginTop: '4px' }}>
                      {new Date(a.firedAt).toLocaleTimeString()} • Severity: {a.severity}
                    </div>
                  </div>
                </div>

                <div>
                  {!a.isAcknowledged && onAcknowledge ? (
                    <button
                      onClick={() => onAcknowledge(a.id)}
                      className="btn-secondary"
                      style={{ padding: '5px 10px', fontSize: '11px' }}
                    >
                      <Check size={12} />
                      <span>Ack</span>
                    </button>
                  ) : (
                    <span style={{ fontSize: '11px', color: 'var(--emerald)', fontWeight: 600 }}>
                      ✓ Ack'd
                    </span>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
};
