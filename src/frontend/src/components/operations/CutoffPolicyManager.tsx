import React, { useState } from 'react';
import { Shield, Plus, Trash2, Sparkles, Play, X, CheckCircle2, AlertTriangle } from 'lucide-react';
import { CutoffPolicyDto, CutoffEventDto, CutoffCheckResultDto } from '../../types';

interface CutoffPolicyManagerProps {
  policies: CutoffPolicyDto[];
  history: CutoffEventDto[];
  processes: string[];
  onSavePolicy: (policy: CutoffPolicyDto) => void;
  onUpdatePolicy: (id: string, policy: CutoffPolicyDto) => void;
  onDeletePolicy: (id: string) => void;
  onTogglePolicy: (id: string, enabled: boolean) => void;
  onEvaluate: () => void;
}

interface CutoffPolicyFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (policy: CutoffPolicyDto) => void;
  processes: string[];
}

const CutoffPolicyForm: React.FC<CutoffPolicyFormProps> = ({
  isOpen, onClose, onSave, processes,
}) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [processName, setProcessName] = useState('');
  const [maxRuntimeSeconds, setMaxRuntimeSeconds] = useState<number>(0);
  const [cutoffTime, setCutoffTime] = useState('');
  const [activeDays, setActiveDays] = useState<number>(0);
  const [forceKill, setForceKill] = useState(false);
  const [notifyEmails, setNotifyEmails] = useState('');

  const dayOptions = [
    { label: 'Mon', bit: 1 },
    { label: 'Tue', bit: 2 },
    { label: 'Wed', bit: 4 },
    { label: 'Thu', bit: 8 },
    { label: 'Fri', bit: 16 },
    { label: 'Sat', bit: 32 },
    { label: 'Sun', bit: 64 },
  ];

  const toggleDay = (bit: number) => {
    if (activeDays === 0) {
      setActiveDays(bit);
    } else if ((activeDays & bit) === bit) {
      setActiveDays(activeDays & ~bit);
    } else {
      setActiveDays(activeDays | bit);
    }
  };

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    onSave({
      name,
      description: description || undefined,
      processName: processName || undefined,
      maxRuntimeSeconds,
      cutoffTime: cutoffTime || undefined,
      activeDays,
      isEnabled: true,
      forceKill,
      notifyEmails: notifyEmails ? notifyEmails.split(',').map(e => e.trim()).filter(e => e) : undefined,
    });
    onClose();
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
            <Shield size={18} color="var(--rose)" />
            <h3 style={{ fontSize: '16px', fontWeight: 700 }}>New Cutoff Policy</h3>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Policy Name *</label>
            <input
              type="text" required value={name} onChange={(e) => setName(e.target.value)}
              placeholder="e.g., End-of-Day Cutoff" className="input-field"
            />
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Description</label>
            <textarea
              value={description} onChange={(e) => setDescription(e.target.value)}
              placeholder="What this policy enforces..."
              className="input-field"
              rows={2}
              style={{ fontSize: '12px' }}
            />
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Scope Process (leave empty = ALL processes)
            </label>
            <select value={processName} onChange={(e) => setProcessName(e.target.value)} className="input-field">
              <option value="">All Processes</option>
              {processes.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
                Max Runtime (seconds)
              </label>
              <input
                type="number" min={0} value={maxRuntimeSeconds}
                onChange={(e) => setMaxRuntimeSeconds(Number(e.target.value))}
                placeholder="0 = no duration limit"
                className="input-field"
              />
              <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>0 = duration-only disabled</span>
            </div>

            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
                Cutoff Time (UTC)
              </label>
              <input
                type="time" value={cutoffTime}
                onChange={(e) => setCutoffTime(e.target.value)}
                className="input-field"
              />
              <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Leave empty = no time-based cutoff</span>
            </div>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Active Days</label>
            <div style={{ display: 'flex', gap: '4px' }}>
              {dayOptions.map((d) => {
                const selected = (activeDays & d.bit) === d.bit;
                return (
                  <button
                    key={d.label}
                    type="button"
                    onClick={() => toggleDay(d.bit)}
                    style={{
                      padding: '4px 10px',
                      borderRadius: '4px',
                      background: selected ? 'rgba(59, 130, 246, 0.2)' : 'rgba(255, 255, 255, 0.04)',
                      border: `1px solid ${selected ? 'var(--primary)' : 'var(--border-subtle)'}`,
                      color: selected ? '#ffffff' : 'var(--text-muted)',
                      fontSize: '11px',
                      cursor: 'pointer',
                    }}
                  >
                    {d.label}
                  </button>
                );
              })}
            </div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Leave all unchecked = every day</span>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
              <input
                type="checkbox" checked={forceKill}
                onChange={(e) => setForceKill(e.target.checked)}
              />
              Force Kill (terminate session vs graceful stop)
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={activeDays !== 0}
                onChange={(e) => setActiveDays(e.target.checked ? 127 : 0)}
              />
              Enable day-of-week filtering
            </label>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Notification Emails (comma-separated)
            </label>
            <input
              type="text" value={notifyEmails}
              onChange={(e) => setNotifyEmails(e.target.value)}
              placeholder="it-support@company.com, noc@company.com"
              className="input-field"
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '14px' }}>
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
            <button type="submit" className="btn-primary">
              <Plus size={14} />
              <span>Save Policy</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export const CutoffPolicyManager: React.FC<CutoffPolicyManagerProps> = ({
  policies, history, processes,
  onSavePolicy, onUpdatePolicy, onDeletePolicy, onTogglePolicy, onEvaluate,
}) => {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isEvaluating, setIsEvaluating] = useState(false);

  const handleEvaluate = async () => {
    setIsEvaluating(true);
    await onEvaluate();
    setIsEvaluating(false);
  };

  const dayLabels = (days: number) => {
    if (days === 0) return 'Every day';
    const labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    const active: string[] = [];
    for (let i = 0; i < 7; i++) {
      if ((days & (1 << i)) !== 0) active.push(labels[i]);
    }
    return active.join(', ');
  };

  const formatCutoffTime = (ts?: string | null) => {
    if (!ts) return 'Duration-only';
    return `${ts} UTC`;
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header */}
      <div
        className="glass-panel"
        style={{
          padding: '24px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '16px',
        }}
      >
        <div>
          <h3 style={{ fontSize: '16px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Shield size={18} color="var(--rose)" />
            Cutoff &amp; Termination Policies
          </h3>
          <p style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
            Define rules that stop specific processes to enforce application maintenance windows and license cutoffs
          </p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button onClick={handleEvaluate} disabled={isEvaluating} className="btn-secondary">
            <Sparkles size={14} color="var(--purple)" />
            <span>{isEvaluating ? 'Evaluating...' : 'Evaluate Now'}</span>
          </button>
          <button onClick={() => setIsFormOpen(true)} className="btn-primary">
            <Plus size={14} />
            <span>New Policy</span>
          </button>
        </div>
      </div>

      {/* Policies List */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {policies.map((p) => (
            <div
              key={p.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '16px',
                background: p.isEnabled ? 'rgba(15, 23, 42, 0.6)' : 'rgba(15, 23, 42, 0.25)',
                border: `1px solid ${p.isEnabled ? 'var(--border-subtle)' : 'rgba(255, 255, 255, 0.04)'}`,
                borderRadius: 'var(--radius-sm)',
                opacity: p.isEnabled ? 1 : 0.6,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div
                  style={{
                    width: '36px', height: '36px', borderRadius: '8px',
                    background: p.isEnabled ? 'rgba(244, 63, 94, 0.15)' : 'rgba(255, 255, 255, 0.04)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}
                >
                  <Shield size={18} color={p.isEnabled ? 'var(--rose)' : 'var(--text-muted)'} />
                </div>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>{p.name}</span>
                    {p.forceKill && (
                      <span style={{
                        fontSize: '10px', color: '#fb7185',
                        background: 'rgba(244, 63, 63, 0.15)',
                        padding: '2px 6px', borderRadius: '4px', fontWeight: 600,
                      }}>Force Kill</span>
                    )}
                  </div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {p.processName ? `Process: ${p.processName}` : 'All Processes'}
                    {' • '}Max runtime: {p.maxRuntimeSeconds > 0 ? `${p.maxRuntimeSeconds}s` : 'N/A'}
                    {' • '}Cutoff: {formatCutoffTime(p.cutoffTime)}
                    {' • '}Days: {dayLabels(p.activeDays)}
                  </div>
                  {p.notifyEmails && p.notifyEmails.length > 0 && (
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
                      Notify: {p.notifyEmails.join(', ')}
                    </div>
                  )}
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <button
                  onClick={() => onTogglePolicy(p.id!, !p.isEnabled)}
                  style={{
                    padding: '6px 14px', borderRadius: 'var(--radius-sm)',
                    background: p.isEnabled ? 'rgba(244, 63, 94, 0.15)' : 'rgba(255, 255, 255, 0.05)',
                    border: `1px solid ${p.isEnabled ? 'rgba(244, 63, 94, 0.3)' : 'var(--border-subtle)'}`,
                    color: p.isEnabled ? '#fb7185' : 'var(--text-muted)',
                    fontSize: '12px', fontWeight: 600, cursor: 'pointer',
                  }}
                >
                  {p.isEnabled ? 'Active' : 'Paused'}
                </button>
                {p.id && (
                  <button
                    onClick={() => onDeletePolicy(p.id!)}
                    style={{
                      background: 'transparent', border: 'none',
                      color: 'var(--text-muted)', cursor: 'pointer', padding: '6px',
                    }}
                    title="Delete Policy"
                  >
                    <Trash2 size={16} />
                  </button>
                )}
              </div>
            </div>
          ))}
          {policies.length === 0 && (
            <div style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
              No cutoff policies configured. Create one to enforce process termination rules.
            </div>
          )}
        </div>
      </div>

      {/* Cutoff History */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>Cutoff Enforcement History</h3>
          <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{history.length} events</span>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {history.map((h) => (
            <div
              key={h.id}
              style={{
                padding: '12px 16px',
                background: 'rgba(15, 23, 42, 0.4)',
                border: `1px solid ${h.stopRequested ? 'rgba(244, 63, 94, 0.3)' : 'rgba(251, 191, 36, 0.3)'}`,
                borderRadius: 'var(--radius-sm)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '12px' }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                    <span style={{ fontSize: '12px', fontWeight: 600, color: '#fb7185' }}>
                      {h.reason === 'DurationExceeded' ? 'Duration Limit' : 'Cutoff Time'}
                    </span>
                    {h.stopRequested ? (
                      <CheckCircle2 size={12} color="var(--emerald)" />
                    ) : (
                      <AlertTriangle size={12} color="var(--amber)" />
                    )}
                  </div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                    {h.processName} on {h.resourceName || '—'}
                  </div>
                  {h.resultMessage && (
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>
                      {h.resultMessage}
                    </div>
                  )}
                </div>
                <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                  {new Date(h.triggerTime).toLocaleString()}
                </span>
              </div>
            </div>
          ))}
          {history.length === 0 && (
            <div style={{ textAlign: 'center', padding: '24px', color: 'var(--text-muted)', fontSize: '12px' }}>
              No cutoff events recorded yet.
            </div>
          )}
        </div>
      </div>

      <CutoffPolicyForm
        isOpen={isFormOpen}
        onClose={() => setIsFormOpen(false)}
        onSave={onSavePolicy}
        processes={processes}
      />
    </div>
  );
};
