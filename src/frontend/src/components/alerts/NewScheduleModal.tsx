import React, { useState } from 'react';
import { X, CalendarClock, Plus } from 'lucide-react';
import { ScheduleDto } from '../../types';

interface NewScheduleModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (schedule: ScheduleDto) => void;
  processes: string[];
  resources: string[];
}

export const NewScheduleModal: React.FC<NewScheduleModalProps> = ({
  isOpen,
  onClose,
  onSave,
  processes,
  resources,
}) => {
  const [name, setName] = useState('');
  const [processName, setProcessName] = useState('');
  const [cronExpression, setCronExpression] = useState('0 9 * * 1-5');
  const [targetResource, setTargetResource] = useState('');
  const [priority, setPriority] = useState(5);
  const [aiOptimizationEnabled, setAiOptimizationEnabled] = useState(true);
  const [businessHoursOnly, setBusinessHoursOnly] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({
      name,
      processName,
      cronExpression,
      targetResource: targetResource || undefined,
      priority,
      aiOptimizationEnabled,
      businessHoursOnly,
      maxRetries: 3,
      isEnabled: true,
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
          maxWidth: '520px',
          padding: '24px',
          background: 'rgba(15, 23, 42, 0.95)',
          border: '1px solid rgba(59, 130, 246, 0.4)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div
              style={{
                width: '32px',
                height: '32px',
                borderRadius: '8px',
                background: 'rgba(59, 130, 246, 0.2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <CalendarClock size={18} color="var(--primary)" />
            </div>
            <h3 style={{ fontSize: '16px', fontWeight: 700 }}>Add Orchestrator Schedule</h3>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Schedule Name *</label>
            <input
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., Hourly Invoice Ingestion"
              className="input-field"
            />
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Process *</label>
            <select
              value={processName}
              onChange={(e) => setProcessName(e.target.value)}
              required
              className="input-field"
            >
              <option value="">Select Blue Prism Process...</option>
              {processes.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Cron Expression *</label>
            <input
              type="text"
              required
              value={cronExpression}
              onChange={(e) => setCronExpression(e.target.value)}
              placeholder="0 9 * * 1-5"
              className="input-field mono"
            />
            <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
              Standard 5-part cron syntax (Minute Hour Day Month DayOfWeek)
            </span>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Resource Target</label>
              <select
                value={targetResource}
                onChange={(e) => setTargetResource(e.target.value)}
                className="input-field"
              >
                <option value="">Auto-Assign (Workload Balancer)</option>
                {resources.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Priority (1-10)</label>
              <input
                type="number"
                min={1}
                max={10}
                value={priority}
                onChange={(e) => setPriority(Number(e.target.value))}
                className="input-field"
              />
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '4px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={aiOptimizationEnabled}
                onChange={(e) => setAiOptimizationEnabled(e.target.checked)}
              />
              <span>Enable AI Window Optimization</span>
            </label>

            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={businessHoursOnly}
                onChange={(e) => setBusinessHoursOnly(e.target.checked)}
              />
              <span>Restrict to Business Hours (08:00 - 18:00)</span>
            </label>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '14px' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" className="btn-primary">
              <Plus size={14} />
              <span>Save Schedule</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
