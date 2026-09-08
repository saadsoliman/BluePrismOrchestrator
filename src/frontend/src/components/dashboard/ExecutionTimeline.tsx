import React from 'react';
import { PlayCircle, CheckCircle2, XCircle, StopCircle, Clock, Cpu } from 'lucide-react';
import { SessionDto } from '../../types';

interface ExecutionTimelineProps {
  sessions: SessionDto[];
  onStopSession?: (sessionId: string, processName: string, resourceName: string) => void;
}

export const ExecutionTimeline: React.FC<ExecutionTimelineProps> = ({ sessions, onStopSession }) => {
  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <PlayCircle size={16} color="var(--primary)" />
            Active & Recent Sessions
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Real-time execution stream from Blue Prism BPASession table
          </p>
        </div>
        <span className="mono" style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
          {sessions.length} sessions tracked
        </span>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {sessions.length === 0 ? (
          <div style={{ padding: '24px', textAlign: 'center', color: 'var(--text-muted)' }}>
            No active sessions running. Use "Dispatch Process" to initiate.
          </div>
        ) : (
          sessions.map((s) => {
            const isRunning = s.status === 'Running';
            const isCompleted = s.status === 'Completed';
            const isFailed = s.status === 'Terminated' || s.status === 'Failed';

            const durationFormatted = s.durationSeconds
              ? `${Math.floor(s.durationSeconds / 60)}m ${Math.floor(s.durationSeconds % 60)}s`
              : 'In progress';

            return (
              <div
                key={s.sessionId}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '12px 16px',
                  background: isRunning ? 'rgba(59, 130, 246, 0.08)' : 'rgba(15, 23, 42, 0.4)',
                  border: `1px solid ${
                    isRunning ? 'rgba(59, 130, 246, 0.3)' : 'var(--border-subtle)'
                  }`,
                  borderRadius: 'var(--radius-sm)',
                  transition: 'all 0.2s ease',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  {isRunning && <PlayCircle size={18} color="var(--cyan)" className="animate-spin" />}
                  {isCompleted && <CheckCircle2 size={18} color="var(--emerald)" />}
                  {isFailed && <XCircle size={18} color="var(--rose)" />}
                  {!isRunning && !isCompleted && !isFailed && <Clock size={18} color="var(--amber)" />}

                  <div>
                    <div style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>
                      {s.processName}
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '11px', color: 'var(--text-muted)' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '3px' }}>
                        <Cpu size={11} /> {s.resourceName}
                      </span>
                      <span>•</span>
                      <span className="mono">
                        {s.startTime ? new Date(s.startTime).toLocaleTimeString() : 'Recent'}
                      </span>
                    </div>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                  <div style={{ textAlign: 'right' }}>
                    <div className="mono" style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-highlight)' }}>
                      {durationFormatted}
                    </div>
                    <div
                      style={{
                        fontSize: '10px',
                        fontWeight: 700,
                        textTransform: 'uppercase',
                        color: isRunning ? 'var(--cyan)' : isCompleted ? 'var(--emerald)' : 'var(--rose)',
                      }}
                    >
                      {s.status}
                    </div>
                  </div>

                  {isRunning && onStopSession && (
                    <button
                      onClick={() => onStopSession(s.sessionId, s.processName, s.resourceName)}
                      className="btn-danger"
                      title="Request Stop via AutomateC"
                    >
                      <StopCircle size={13} />
                      <span>Stop</span>
                    </button>
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
