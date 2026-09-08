import React, { useState } from 'react';
import { CalendarClock, Sparkles, Check, Play, Plus, Clock, Cpu, Trash2 } from 'lucide-react';
import { ScheduleDto, ScheduleOptimizationResultDto } from '../../types';

interface ScheduleManagerProps {
  schedules: ScheduleDto[];
  onToggleSchedule: (schedule: ScheduleDto) => void;
  onDeleteSchedule: (id: string) => void;
  onOptimize: () => void;
  optimizations: ScheduleOptimizationResultDto[];
  onOpenCreateModal: () => void;
}

export const ScheduleManager: React.FC<ScheduleManagerProps> = ({
  schedules,
  onToggleSchedule,
  onDeleteSchedule,
  onOptimize,
  optimizations,
  onOpenCreateModal,
}) => {
  const [isOptimizing, setIsOptimizing] = useState(false);

  const handleOptimizeClick = async () => {
    setIsOptimizing(true);
    await onOptimize();
    setIsOptimizing(false);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Top Header Card */}
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
            <CalendarClock size={18} color="var(--primary)" />
            Orchestrator Process Schedules
          </h3>
          <p style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
            Hangfire cron engine with dynamic AI conflict avoidance & SLA monitoring
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <button onClick={handleOptimizeClick} disabled={isOptimizing} className="btn-secondary">
            <Sparkles size={14} color="var(--purple)" />
            <span>{isOptimizing ? 'Analyzing...' : 'AI Schedule Optimizer'}</span>
          </button>
          <button onClick={onOpenCreateModal} className="btn-primary">
            <Plus size={14} />
            <span>New Schedule</span>
          </button>
        </div>
      </div>

      {/* Optimization recommendations if generated */}
      {optimizations.length > 0 && (
        <div
          className="glass-panel"
          style={{
            padding: '20px',
            border: '1px solid rgba(168, 85, 247, 0.4)',
            background: 'rgba(168, 85, 247, 0.06)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px' }}>
            <Sparkles size={16} color="var(--purple)" />
            <span style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>
              AI Schedule Optimization Insights
            </span>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            {optimizations.map((opt) => (
              <div
                key={opt.scheduleId}
                style={{
                  padding: '12px 16px',
                  background: 'rgba(15, 23, 42, 0.6)',
                  borderRadius: 'var(--radius-sm)',
                  border: '1px solid var(--border-subtle)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <div>
                  <div style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>
                    {opt.processName}
                  </div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {opt.reasoning}
                  </div>
                  <div style={{ fontSize: '11px', color: '#34d399', marginTop: '4px' }}>
                    Impact: {opt.estimatedImpact}
                  </div>
                </div>

                <div style={{ textAlign: 'right' }}>
                  <div className="mono" style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                    Current: {opt.originalCron}
                  </div>
                  <div className="mono" style={{ fontSize: '12px', fontWeight: 700, color: 'var(--cyan)' }}>
                    Suggested: {opt.suggestedCron}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Schedules List */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {schedules.map((s) => (
            <div
              key={s.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '16px',
                background: s.isEnabled ? 'rgba(15, 23, 42, 0.6)' : 'rgba(15, 23, 42, 0.25)',
                border: `1px solid ${s.isEnabled ? 'var(--border-subtle)' : 'rgba(255, 255, 255, 0.04)'}`,
                borderRadius: 'var(--radius-sm)',
                opacity: s.isEnabled ? 1 : 0.6,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div
                  style={{
                    width: '36px',
                    height: '36px',
                    borderRadius: '8px',
                    background: s.isEnabled ? 'rgba(59, 130, 246, 0.15)' : 'rgba(255, 255, 255, 0.04)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  <Clock size={18} color={s.isEnabled ? 'var(--primary)' : 'var(--text-muted)'} />
                </div>

                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>{s.name}</span>
                    {s.aiOptimizationEnabled && (
                      <span
                        style={{
                          fontSize: '10px',
                          color: 'var(--purple)',
                          background: 'rgba(168, 85, 247, 0.15)',
                          padding: '2px 6px',
                          borderRadius: '4px',
                          fontWeight: 600,
                          display: 'flex',
                          alignItems: 'center',
                          gap: '3px',
                        }}
                      >
                        <Sparkles size={9} /> AI Managed
                      </span>
                    )}
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px', fontSize: '12px', color: 'var(--text-secondary)', marginTop: '4px' }}>
                    <span>Process: <strong style={{ color: '#ffffff' }}>{s.processName}</strong></span>
                    <span>•</span>
                    <span className="mono">Cron: {s.cronExpression || 'Manual'}</span>
                    <span>•</span>
                    <span>Priority: {s.priority}</span>
                    {s.targetResource && (
                      <>
                        <span>•</span>
                        <span>Bot: {s.targetResource}</span>
                      </>
                    )}
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <button
                  onClick={() => onToggleSchedule({ ...s, isEnabled: !s.isEnabled })}
                  style={{
                    padding: '6px 14px',
                    borderRadius: 'var(--radius-sm)',
                    background: s.isEnabled ? 'rgba(16, 185, 129, 0.15)' : 'rgba(255, 255, 255, 0.05)',
                    border: `1px solid ${s.isEnabled ? 'rgba(16, 185, 129, 0.3)' : 'var(--border-subtle)'}`,
                    color: s.isEnabled ? '#34d399' : 'var(--text-muted)',
                    fontSize: '12px',
                    fontWeight: 600,
                    cursor: 'pointer',
                  }}
                >
                  {s.isEnabled ? 'Enabled' : 'Paused'}
                </button>

                {s.id && (
                  <button
                    onClick={() => onDeleteSchedule(s.id!)}
                    style={{
                      background: 'transparent',
                      border: 'none',
                      color: 'var(--text-muted)',
                      cursor: 'pointer',
                      padding: '6px',
                    }}
                    title="Delete Schedule"
                  >
                    <Trash2 size={16} />
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
