import React from 'react';
import { Award, AlertTriangle, CheckCircle2, TrendingUp } from 'lucide-react';
import { SlaComplianceDto } from '../../types';

interface SLAComplianceProps {
  data: SlaComplianceDto;
}

export const SLACompliance: React.FC<SLAComplianceProps> = ({ data }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Award size={16} color="var(--emerald)" />
            Enterprise SLA Compliance
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Turnaround time adherence against configured process SLAs
          </p>
        </div>
      </div>

      {/* KPI row */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', marginBottom: '20px' }}>
        <div style={{ background: 'rgba(16, 185, 129, 0.1)', padding: '16px', borderRadius: 'var(--radius-sm)' }}>
          <div style={{ fontSize: '11px', color: '#34d399', fontWeight: 600 }}>Overall Compliance</div>
          <div className="mono" style={{ fontSize: '24px', fontWeight: 800, color: '#ffffff' }}>
            {data.overallCompliancePercent}%
          </div>
        </div>

        <div style={{ background: 'rgba(59, 130, 246, 0.1)', padding: '16px', borderRadius: 'var(--radius-sm)' }}>
          <div style={{ fontSize: '11px', color: 'var(--primary-light)', fontWeight: 600 }}>Total Executions</div>
          <div className="mono" style={{ fontSize: '24px', fontWeight: 800, color: '#ffffff' }}>
            {data.totalExecutions}
          </div>
        </div>

        <div style={{ background: 'rgba(244, 63, 94, 0.1)', padding: '16px', borderRadius: 'var(--radius-sm)' }}>
          <div style={{ fontSize: '11px', color: '#fb7185', fontWeight: 600 }}>SLA Breaches</div>
          <div className="mono" style={{ fontSize: '24px', fontWeight: 800, color: '#ffffff' }}>
            {data.slaBreaches}
          </div>
        </div>
      </div>

      {/* Worst processes list */}
      <h4 style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '10px' }}>
        SLA Risk Watchlist
      </h4>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {data.worstProcesses.map((p) => (
          <div
            key={p.processName}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '10px 14px',
              background: 'rgba(15, 23, 42, 0.4)',
              border: '1px solid var(--border-subtle)',
              borderRadius: 'var(--radius-sm)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <AlertTriangle size={14} color="var(--amber)" />
              <span style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>{p.processName}</span>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
              <span className="mono" style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                {p.breaches} breaches
              </span>
              <span
                className="mono"
                style={{
                  fontSize: '12px',
                  fontWeight: 700,
                  color: p.compliancePercent > 95 ? 'var(--emerald)' : 'var(--amber)',
                }}
              >
                {p.compliancePercent}%
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
