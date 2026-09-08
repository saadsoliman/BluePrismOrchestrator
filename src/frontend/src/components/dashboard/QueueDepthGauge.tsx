import React from 'react';
import { Layers, ShieldCheck, AlertCircle } from 'lucide-react';
import { QueueSummaryDto } from '../../types';

interface QueueDepthGaugeProps {
  queues: QueueSummaryDto[];
}

export const QueueDepthGauge: React.FC<QueueDepthGaugeProps> = ({ queues }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Layers size={16} color="var(--purple)" />
            Work Queue Health & Intake
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Real-time item distribution across Blue Prism Work Queues
          </p>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {queues.map((q) => {
          const isHighBacklog = q.pending > 100;
          return (
            <div
              key={q.name}
              style={{
                background: 'rgba(15, 23, 42, 0.5)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-sm)',
                padding: '14px',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>{q.name}</span>
                  {isHighBacklog && (
                    <span
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '4px',
                        fontSize: '10px',
                        color: 'var(--amber)',
                        background: 'rgba(245, 158, 11, 0.15)',
                        padding: '2px 6px',
                        borderRadius: '4px',
                        fontWeight: 600,
                      }}
                    >
                      <AlertCircle size={10} /> Backlog Warning
                    </span>
                  )}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <ShieldCheck size={14} color="var(--emerald)" />
                  <span className="mono" style={{ fontSize: '12px', fontWeight: 700, color: '#34d399' }}>
                    {q.healthPercent}% Success
                  </span>
                </div>
              </div>

              {/* Status pills breakdown */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(4, 1fr)',
                  gap: '8px',
                  marginBottom: '10px',
                  textAlign: 'center',
                }}
              >
                <div style={{ background: 'rgba(245, 158, 11, 0.1)', padding: '6px', borderRadius: '4px' }}>
                  <div style={{ fontSize: '10px', color: 'var(--amber)', fontWeight: 600 }}>Pending</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: '#fbbf24' }}>
                    {q.pending}
                  </div>
                </div>

                <div style={{ background: 'rgba(6, 182, 212, 0.1)', padding: '6px', borderRadius: '4px' }}>
                  <div style={{ fontSize: '10px', color: 'var(--cyan)', fontWeight: 600 }}>Locked</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: '#38bdf8' }}>
                    {q.locked}
                  </div>
                </div>

                <div style={{ background: 'rgba(16, 185, 129, 0.1)', padding: '6px', borderRadius: '4px' }}>
                  <div style={{ fontSize: '10px', color: 'var(--emerald)', fontWeight: 600 }}>Completed</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: '#34d399' }}>
                    {q.completed}
                  </div>
                </div>

                <div style={{ background: 'rgba(244, 63, 94, 0.1)', padding: '6px', borderRadius: '4px' }}>
                  <div style={{ fontSize: '10px', color: 'var(--rose)', fontWeight: 600 }}>Exceptions</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: '#fb7185' }}>
                    {q.exceptioned}
                  </div>
                </div>
              </div>

              {/* Visual health bar */}
              <div className="progress-track" style={{ height: '4px' }}>
                <div
                  className={`progress-fill ${q.healthPercent > 95 ? 'success' : q.healthPercent > 80 ? 'warning' : 'danger'}`}
                  style={{ width: `${q.healthPercent}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
