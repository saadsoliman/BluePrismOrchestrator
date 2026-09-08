import React, { useState } from 'react';
import { Bell, Plus, Shield, Check, X } from 'lucide-react';
import { AlertRuleDto, AlertRuleType, AlertSeverity, ComparisonOperator, NotificationChannel } from '../../types';

interface AlertRuleBuilderProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (rule: AlertRuleDto) => void;
  processes: string[];
}

export const AlertRuleBuilder: React.FC<AlertRuleBuilderProps> = ({
  isOpen,
  onClose,
  onSave,
  processes,
}) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [ruleType, setRuleType] = useState<AlertRuleType>('Threshold');
  const [severity, setSeverity] = useState<AlertSeverity>('Warning');
  const [processName, setProcessName] = useState('');
  const [metricName, setMetricName] = useState('PendingQueueItems');
  const [operator, setOperator] = useState<ComparisonOperator>('GreaterThan');
  const [thresholdValue, setThresholdValue] = useState<number>(100);
  const [channels, setChannels] = useState<NotificationChannel[]>(['InApp']);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      name,
      description,
      ruleType,
      severity,
      processName: processName ? processName : undefined,
      metricName,
      operator,
      thresholdValue,
      cooldownMinutes: 15,
      channels,
      isEnabled: true,
    });
    onClose();
  };

  const toggleChannel = (ch: NotificationChannel) => {
    if (channels.includes(ch)) {
      setChannels(channels.filter((c) => c !== ch));
    } else {
      setChannels([...channels, ch]);
    }
  };

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.75)',
        backdropFilter: 'blur(8px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 50,
        padding: '20px',
      }}
    >
      <div
        className="glass-panel"
        style={{
          width: '100%',
          maxWidth: '560px',
          padding: '24px',
          background: 'rgba(15, 23, 42, 0.95)',
          border: '1px solid rgba(244, 63, 94, 0.4)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div
              style={{
                width: '32px',
                height: '32px',
                borderRadius: '8px',
                background: 'rgba(244, 63, 94, 0.2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Bell size={18} color="var(--rose)" />
            </div>
            <h3 style={{ fontSize: '16px', fontWeight: 700 }}>New Alert Rule</h3>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Rule Name *</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., High Invoice Backlog"
              className="input-field"
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Rule Type</label>
              <select
                value={ruleType}
                onChange={(e) => setRuleType(e.target.value as AlertRuleType)}
                className="input-field"
              >
                <option value="Threshold">Threshold Rule</option>
                <option value="Anomaly">AI Anomaly Rule</option>
                <option value="SlaBreach">SLA Breach Rule</option>
                <option value="ResourceDown">Resource Down Rule</option>
                <option value="ConsecutiveFailures">Consecutive Failures Rule</option>
              </select>
            </div>

            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Severity</label>
              <select
                value={severity}
                onChange={(e) => setSeverity(e.target.value as AlertSeverity)}
                className="input-field"
              >
                <option value="Info">Info</option>
                <option value="Warning">Warning</option>
                <option value="Critical">Critical</option>
              </select>
            </div>
          </div>

          {ruleType === 'Threshold' && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px' }}>
              <div>
                <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Metric</label>
                <select
                  value={metricName}
                  onChange={(e) => setMetricName(e.target.value)}
                  className="input-field"
                >
                  <option value="PendingQueueItems">Queue Backlog</option>
                  <option value="FailureRate">Failure Rate %</option>
                  <option value="ExecutionDuration">Duration (s)</option>
                </select>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Operator</label>
                <select
                  value={operator}
                  onChange={(e) => setOperator(e.target.value as ComparisonOperator)}
                  className="input-field"
                >
                  <option value="GreaterThan">&gt; Greater Than</option>
                  <option value="GreaterThanOrEqual">&gt;= Greater/Equal</option>
                  <option value="LessThan">&lt; Less Than</option>
                </select>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Threshold</label>
                <input
                  type="number"
                  value={thresholdValue}
                  onChange={(e) => setThresholdValue(Number(e.target.value))}
                  className="input-field"
                />
              </div>
            </div>
          )}

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Scope Process (Optional)
            </label>
            <select
              value={processName}
              onChange={(e) => setProcessName(e.target.value)}
              className="input-field"
            >
              <option value="">All Processes</option>
              {processes.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Notification Channels
            </label>
            <div style={{ display: 'flex', gap: '8px' }}>
              {(['InApp', 'Email', 'Teams', 'Webhook'] as NotificationChannel[]).map((ch) => {
                const isSelected = channels.includes(ch);
                return (
                  <button
                    key={ch}
                    type="button"
                    onClick={() => toggleChannel(ch)}
                    style={{
                      padding: '6px 12px',
                      borderRadius: 'var(--radius-sm)',
                      background: isSelected ? 'rgba(59, 130, 246, 0.2)' : 'rgba(255, 255, 255, 0.05)',
                      border: `1px solid ${isSelected ? 'var(--primary)' : 'var(--border-subtle)'}`,
                      color: isSelected ? '#ffffff' : 'var(--text-muted)',
                      fontSize: '12px',
                      cursor: 'pointer',
                    }}
                  >
                    {ch}
                  </button>
                );
              })}
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '14px' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" className="btn-primary">
              <Plus size={14} />
              <span>Create Rule</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
