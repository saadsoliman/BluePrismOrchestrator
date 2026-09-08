import React, { useState } from 'react';
import { Bell, Plus, ShieldCheck, AlertCircle, AlertTriangle, Info, Trash2, Check } from 'lucide-react';
import { AlertRuleBuilder } from '../components/alerts/AlertRuleBuilder';
import { AlertRuleDto, AlertHistoryDto } from '../types';

interface AlertsPageProps {
  rules: AlertRuleDto[];
  history: AlertHistoryDto[];
  onSaveRule: (rule: AlertRuleDto) => void;
  onDeleteRule: (id: string) => void;
  onToggleRule: (rule: AlertRuleDto) => void;
  onAcknowledgeAlert: (id: string) => void;
  processes: string[];
}

export const AlertsPage: React.FC<AlertsPageProps> = ({
  rules,
  history,
  onSaveRule,
  onDeleteRule,
  onToggleRule,
  onAcknowledgeAlert,
  processes,
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filterSeverity, setFilterSeverity] = useState<string>('ALL');

  const filteredHistory = history.filter((h) => {
    if (filterSeverity === 'ALL') return true;
    return h.severity === filterSeverity;
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {/* Rules Section */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
          <div>
            <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
              <ShieldCheck size={16} color="var(--primary)" />
              Configured Alert Rules ({rules.length})
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              Operational threshold policies, consecutive failure monitors, and AI anomaly detectors
            </p>
          </div>

          <button onClick={() => setIsModalOpen(true)} className="btn-primary">
            <Plus size={14} />
            <span>New Rule</span>
          </button>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '12px' }}>
          {rules.map((rule) => (
            <div
              key={rule.id}
              style={{
                padding: '16px',
                background: rule.isEnabled ? 'rgba(15, 23, 42, 0.6)' : 'rgba(15, 23, 42, 0.25)',
                border: `1px solid ${rule.isEnabled ? 'var(--border-subtle)' : 'rgba(255, 255, 255, 0.04)'}`,
                borderRadius: 'var(--radius-sm)',
                opacity: rule.isEnabled ? 1 : 0.6,
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '8px' }}>
                <span style={{ fontSize: '13px', fontWeight: 700, color: '#ffffff' }}>{rule.name}</span>
                <span
                  style={{
                    fontSize: '10px',
                    fontWeight: 700,
                    textTransform: 'uppercase',
                    color: rule.severity === 'Critical' ? 'var(--rose)' : 'var(--amber)',
                    background: rule.severity === 'Critical' ? 'rgba(244, 63, 94, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                    padding: '2px 6px',
                    borderRadius: '4px',
                  }}
                >
                  {rule.severity}
                </span>
              </div>

              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginBottom: '12px' }}>
                {rule.description || `${rule.ruleType} monitor`}
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div style={{ display: 'flex', gap: '4px' }}>
                  {rule.channels.map((ch) => (
                    <span
                      key={ch}
                      style={{
                        fontSize: '10px',
                        background: 'rgba(255, 255, 255, 0.06)',
                        padding: '2px 6px',
                        borderRadius: '4px',
                        color: 'var(--text-muted)',
                      }}
                    >
                      {ch}
                    </span>
                  ))}
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <button
                    onClick={() => onToggleRule({ ...rule, isEnabled: !rule.isEnabled })}
                    style={{
                      padding: '4px 10px',
                      borderRadius: '4px',
                      border: '1px solid var(--border-subtle)',
                      background: rule.isEnabled ? 'rgba(16, 185, 129, 0.15)' : 'rgba(255, 255, 255, 0.05)',
                      color: rule.isEnabled ? '#34d399' : 'var(--text-muted)',
                      fontSize: '11px',
                      fontWeight: 600,
                      cursor: 'pointer',
                    }}
                  >
                    {rule.isEnabled ? 'Active' : 'Muted'}
                  </button>
                  {rule.id && (
                    <button
                      onClick={() => onDeleteRule(rule.id!)}
                      style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Alert Audit Trail */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
          <div>
            <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Bell size={16} color="var(--rose)" />
              Incident Audit Trail & History
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              Historical log of fired alert notifications across all channels
            </p>
          </div>

          <div style={{ display: 'flex', gap: '6px' }}>
            {['ALL', 'Critical', 'Warning', 'Info'].map((sev) => (
              <button
                key={sev}
                onClick={() => setFilterSeverity(sev)}
                style={{
                  padding: '4px 10px',
                  borderRadius: '4px',
                  border: '1px solid var(--border-subtle)',
                  background: filterSeverity === sev ? 'var(--primary)' : 'rgba(255, 255, 255, 0.04)',
                  color: filterSeverity === sev ? '#ffffff' : 'var(--text-secondary)',
                  fontSize: '11px',
                  fontWeight: 600,
                  cursor: 'pointer',
                }}
              >
                {sev}
              </button>
            ))}
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {filteredHistory.map((h) => {
            const isCritical = h.severity === 'Critical';
            const isWarning = h.severity === 'Warning';

            return (
              <div
                key={h.id}
                style={{
                  padding: '12px 16px',
                  background: 'rgba(15, 23, 42, 0.5)',
                  border: '1px solid var(--border-subtle)',
                  borderRadius: 'var(--radius-sm)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  {isCritical && <AlertCircle size={16} color="var(--rose)" />}
                  {isWarning && <AlertTriangle size={16} color="var(--amber)" />}
                  {!isCritical && !isWarning && <Info size={16} color="var(--primary)" />}

                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>{h.ruleName}</span>
                      <span
                        style={{
                          fontSize: '10px',
                          color: isCritical ? '#fb7185' : '#fbbf24',
                          background: isCritical ? 'rgba(244, 63, 94, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                          padding: '2px 6px',
                          borderRadius: '4px',
                          fontWeight: 700,
                        }}
                      >
                        {h.severity}
                      </span>
                    </div>
                    <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                      {h.message}
                    </div>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                  <span className="mono" style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                    {new Date(h.firedAt).toLocaleString()}
                  </span>

                  {!h.isAcknowledged ? (
                    <button
                      onClick={() => onAcknowledgeAlert(h.id)}
                      className="btn-secondary"
                      style={{ padding: '4px 10px', fontSize: '11px' }}
                    >
                      <Check size={12} />
                      <span>Acknowledge</span>
                    </button>
                  ) : (
                    <span style={{ fontSize: '11px', color: 'var(--emerald)', fontWeight: 600 }}>
                      ✓ Resolved
                    </span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <AlertRuleBuilder
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={onSaveRule}
        processes={processes}
      />
    </div>
  );
};
