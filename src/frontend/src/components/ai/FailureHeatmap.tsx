import React from 'react';
import { Grid, Info } from 'lucide-react';
import { FailurePredictionDto } from '../../types';

interface FailureHeatmapProps {
  data: FailurePredictionDto[];
}

export const FailureHeatmap: React.FC<FailureHeatmapProps> = ({ data }) => {
  // Group predictions by process
  const processes = Array.from(new Set(data.map((d) => d.processName)));
  const hours = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22];

  const getColor = (prob: number) => {
    if (prob < 8) return 'rgba(16, 185, 129, 0.25)';
    if (prob < 15) return 'rgba(6, 182, 212, 0.35)';
    if (prob < 25) return 'rgba(245, 158, 11, 0.45)';
    return 'rgba(244, 63, 94, 0.65)';
  };

  const getTextColor = (prob: number) => {
    if (prob < 8) return '#34d399';
    if (prob < 15) return '#38bdf8';
    if (prob < 25) return '#fbbf24';
    return '#fb7185';
  };

  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Grid size={16} color="var(--primary)" />
            AI Failure Risk Heatmap (24h Window)
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            ML.NET binary classification prediction of failure probability by process and time-of-day
          </p>
        </div>

        {/* Legend */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', fontSize: '11px', color: 'var(--text-muted)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <div style={{ width: '10px', height: '10px', background: 'rgba(16, 185, 129, 0.6)', borderRadius: '2px' }} />
            <span>&lt;8% Low</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <div style={{ width: '10px', height: '10px', background: 'rgba(245, 158, 11, 0.6)', borderRadius: '2px' }} />
            <span>15-25% Med</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <div style={{ width: '10px', height: '10px', background: 'rgba(244, 63, 94, 0.8)', borderRadius: '2px' }} />
            <span>&gt;25% High</span>
          </div>
        </div>
      </div>

      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: '11px', color: 'var(--text-muted)' }}>
                Process Name
              </th>
              {hours.map((h) => (
                <th
                  key={h}
                  style={{
                    textAlign: 'center',
                    padding: '8px 6px',
                    fontSize: '11px',
                    color: 'var(--text-muted)',
                    fontFamily: 'var(--font-mono)',
                  }}
                >
                  {String(h).padStart(2, '0')}:00
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {processes.map((proc) => (
              <tr key={proc} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.04)' }}>
                <td style={{ padding: '10px 12px', fontSize: '12px', fontWeight: 600, color: '#ffffff', whiteSpace: 'nowrap' }}>
                  {proc}
                </td>
                {hours.map((h) => {
                  const match = data.find((d) => d.processName === proc && d.hour === h);
                  const prob = match ? match.failureProbability : 5.0;
                  return (
                    <td key={h} style={{ padding: '6px' }}>
                      <div
                        style={{
                          background: getColor(prob),
                          border: `1px solid ${getTextColor(prob)}33`,
                          borderRadius: '6px',
                          padding: '8px 4px',
                          textAlign: 'center',
                          fontSize: '11px',
                          fontWeight: 700,
                          color: getTextColor(prob),
                          fontFamily: 'var(--font-mono)',
                          transition: 'transform 0.2s ease',
                          cursor: 'pointer',
                        }}
                        title={`${proc} at ${h}:00 — ${prob}% predicted failure probability`}
                      >
                        {prob.toFixed(0)}%
                      </div>
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '14px', fontSize: '11px', color: 'var(--text-muted)' }}>
        <Info size={13} />
        <span>Peak contention zones (09:00 - 11:00 UTC) show increased failure risk due to target application latency.</span>
      </div>
    </div>
  );
};
