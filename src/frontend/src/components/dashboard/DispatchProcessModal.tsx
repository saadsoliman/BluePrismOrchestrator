import React, { useState } from 'react';
import { X, Play, Cpu, CheckCircle2, AlertTriangle, Loader2 } from 'lucide-react';
import { api } from '../../api/client';
import { ProcessDto, ResourceStatusDto } from '../../types';

interface DispatchProcessModalProps {
  isOpen: boolean;
  onClose: () => void;
  processes: ProcessDto[];
  resources: ResourceStatusDto[];
  onDispatched?: () => void;
}

export const DispatchProcessModal: React.FC<DispatchProcessModalProps> = ({
  isOpen,
  onClose,
  processes,
  resources,
  onDispatched,
}) => {
  const [selectedProcess, setSelectedProcess] = useState<string>('');
  const [selectedResource, setSelectedResource] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<{ success: boolean; message: string } | null>(null);

  if (!isOpen) return null;

  const handleDispatch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProcess) return;

    setLoading(true);
    setResult(null);

    try {
      const res = await api.runProcess(
        selectedProcess,
        selectedResource ? selectedResource : undefined
      );
      setResult({
        success: true,
        message: res.message || `Execution dispatched successfully (ID: ${res.executionId.slice(0, 8)}...)`,
      });
      if (onDispatched) onDispatched();
    } catch (err: any) {
      setResult({
        success: false,
        message: err.message || 'Failed to dispatch process',
      });
    } finally {
      setLoading(false);
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
          maxWidth: '520px',
          padding: '24px',
          background: 'rgba(15, 23, 42, 0.95)',
          border: '1px solid rgba(59, 130, 246, 0.4)',
          boxShadow: '0 20px 50px rgba(0, 0, 0, 0.8), 0 0 30px rgba(59, 130, 246, 0.2)',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
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
              <Cpu size={18} color="var(--primary)" />
            </div>
            <div>
              <h3 style={{ fontSize: '16px', fontWeight: 700 }}>Dispatch RPA Execution</h3>
              <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Trigger process via AutomateC.exe integration</p>
            </div>
          </div>
          <button
            onClick={onClose}
            style={{
              background: 'transparent',
              border: 'none',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              padding: '4px',
            }}
          >
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleDispatch} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Target Process *
            </label>
            <select
              value={selectedProcess}
              onChange={(e) => setSelectedProcess(e.target.value)}
              required
              className="input-field"
              style={{ cursor: 'pointer' }}
            >
              <option value="">Select a Blue Prism process...</option>
              {processes.map((p) => (
                <option key={p.processId} value={p.name}>
                  {p.name} ({p.successRate}% success, ~{p.avgDurationSeconds.toFixed(0)}s)
                </option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
              Runtime Resource (Bot)
            </label>
            <select
              value={selectedResource}
              onChange={(e) => setSelectedResource(e.target.value)}
              className="input-field"
              style={{ cursor: 'pointer' }}
            >
              <option value="">⚡ Auto-Assign (Smart Workload Balancer)</option>
              {resources.map((r) => (
                <option key={r.name} value={r.name}>
                  {r.name} — {r.isOnline ? 'Online' : 'Offline'} ({r.activeSessions} active)
                </option>
              ))}
            </select>
            <p style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
              If unassigned, the workload balancer assigns the least loaded online resource.
            </p>
          </div>

          {result && (
            <div
              style={{
                padding: '12px',
                borderRadius: 'var(--radius-sm)',
                background: result.success ? 'rgba(16, 185, 129, 0.15)' : 'rgba(244, 63, 94, 0.15)',
                border: `1px solid ${result.success ? 'rgba(16, 185, 129, 0.3)' : 'rgba(244, 63, 94, 0.3)'}`,
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                fontSize: '12px',
                color: result.success ? '#34d399' : '#fb7185',
              }}
            >
              {result.success ? <CheckCircle2 size={16} /> : <AlertTriangle size={16} />}
              <span>{result.message}</span>
            </div>
          )}

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" disabled={loading || !selectedProcess} className="btn-primary">
              {loading ? <Loader2 size={14} className="animate-spin" /> : <Play size={14} fill="currentColor" />}
              <span>{loading ? 'Dispatching...' : 'Dispatch Process'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
