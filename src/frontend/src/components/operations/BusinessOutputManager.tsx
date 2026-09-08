import React, { useState } from 'react';
import { FileSpreadsheet, Plus, Trash2, CheckCircle2, AlertTriangle, X, Send } from 'lucide-react';
import { BusinessOutputRuleDto, BusinessOutputDeliveryLogDto } from '../../types';

interface BusinessOutputManagerProps {
  rules: BusinessOutputRuleDto[];
  deliveryLogs: BusinessOutputDeliveryLogDto[];
  processes: string[];
  onSaveRule: (rule: BusinessOutputRuleDto) => void;
  onUpdateRule: (id: string, rule: BusinessOutputRuleDto) => void;
  onDeleteRule: (id: string) => void;
  onToggleRule: (id: string, enabled: boolean) => void;
  onDeliver: () => void;
}

interface BusinessOutputRuleFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (rule: BusinessOutputRuleDto) => void;
  processes: string[];
}

const BusinessOutputRuleForm: React.FC<BusinessOutputRuleFormProps> = ({
  isOpen, onClose, onSave, processes,
}) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [processName, setProcessName] = useState('');
  const [outputType, setOutputType] = useState('Excel');
  const [fileNamePattern, setFileNamePattern] = useState('');
  const [deliveryMethod, setDeliveryMethod] = useState('Email');
  const [deliveryTarget, setDeliveryTarget] = useState('');
  const [deliverOnFailure, setDeliverOnFailure] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !processName) return;

    onSave({
      name,
      description: description || undefined,
      processName,
      outputType,
      fileNamePattern: fileNamePattern || undefined,
      deliveryMethod,
      deliveryTarget,
      deliverOnFailure,
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
          maxWidth: '560px',
          padding: '24px',
          background: 'rgba(15, 23, 42, 0.95)',
          border: '1px solid rgba(59, 130, 246, 0.4)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <FileSpreadsheet size={18} color="var(--primary)" />
            <h3 style={{ fontSize: '16px', fontWeight: 700 }}>New Business Output Rule</h3>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Rule Name *</label>
            <input
              type="text" required value={name} onChange={(e) => setName(e.target.value)}
              placeholder="e.g., Invoice Excel Delivery to Finance"
              className="input-field"
            />
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Description</label>
            <textarea
              value={description} onChange={(e) => setDescription(e.target.value)}
              placeholder="What outputs this rule delivers..."
              className="input-field"
              rows={2}
              style={{ fontSize: '12px' }}
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
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Output Type</label>
              <select value={outputType} onChange={(e) => setOutputType(e.target.value)} className="input-field">
                <option value="Excel">Excel (.xlsx)</option>
                <option value="Pdf">PDF (.pdf)</option>
                <option value="Csv">CSV (.csv)</option>
              </select>
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>File Pattern</label>
              <input
                type="text" value={fileNamePattern}
                onChange={(e) => setFileNamePattern(e.target.value)}
                placeholder="*.xlsx or Report_*.xlsx"
                className="input-field mono"
              />
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>Delivery Method</label>
              <select value={deliveryMethod} onChange={(e) => setDeliveryMethod(e.target.value)} className="input-field">
                <option value="Email">Email</option>
                <option value="SharePoint">SharePoint</option>
                <option value="TeamsChannel">Teams Channel</option>
                <option value="SmbShare">SMB File Share</option>
              </select>
            </div>
            <div>
              <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, marginBottom: '6px' }}>
                Delivery Target
              </label>
              <input
                type="text" value={deliveryTarget}
                onChange={(e) => setDeliveryTarget(e.target.value)}
                placeholder={deliveryMethod === 'Email' ? 'email@company.com, team@company.com' : '\\\\server\\share\\path or URL'}
                required
                className="input-field mono"
                style={{ fontSize: '12px' }}
              />
            </div>
          </div>

          <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
            <input
              type="checkbox" checked={deliverOnFailure}
              onChange={(e) => setDeliverOnFailure(e.target.checked)}
            />
            <span>Deliver even on process failure</span>
          </label>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '14px' }}>
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
            <button type="submit" className="btn-primary">
              <Plus size={14} />
              <span>Save Rule</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export const BusinessOutputManager: React.FC<BusinessOutputManagerProps> = ({
  rules, deliveryLogs, processes,
  onSaveRule, onUpdateRule, onDeleteRule, onToggleRule, onDeliver,
}) => {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDelivering, setIsDelivering] = useState(false);

  const handleDeliver = async () => {
    setIsDelivering(true);
    await onDeliver();
    setIsDelivering(false);
  };

  const deliveryMethodLabel = (method: string) => {
    return method === 'Email' ? '📧 Email' :
           method === 'SharePoint' ? '📂 SharePoint' :
           method === 'TeamsChannel' ? '💬 Teams' :
           method === 'SmbShare' ? '📁 SMB Share' : method;
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header */}
      <div className="glass-panel" style={{ padding: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h3 style={{ fontSize: '16px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <FileSpreadsheet size={18} color="var(--primary)" />
            Business Output Delivery Rules
          </h3>
          <p style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
            Automatically deliver Excel and other process outputs to Business stakeholders on completion
          </p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button onClick={handleDeliver} disabled={isDelivering} className="btn-secondary">
            <Send size={14} color="var(--cyan)" />
            <span>{isDelivering ? 'Delivering...' : 'Run Delivery Now'}</span>
          </button>
          <button onClick={() => setIsFormOpen(true)} className="btn-primary">
            <Plus size={14} />
            <span>New Rule</span>
          </button>
        </div>
      </div>

      {/* Rules List */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {rules.map((r) => (
            <div
              key={r.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '16px',
                background: r.isEnabled ? 'rgba(15, 23, 42, 0.6)' : 'rgba(15, 23, 42, 0.25)',
                border: `1px solid ${r.isEnabled ? 'var(--border-subtle)' : 'rgba(255, 255, 255, 0.04)'}`,
                borderRadius: 'var(--radius-sm)',
                opacity: r.isEnabled ? 1 : 0.6,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                <div
                  style={{
                    width: '36px', height: '36px', borderRadius: '8px',
                    background: r.isEnabled ? 'rgba(59, 130, 246, 0.15)' : 'rgba(255, 255, 255, 0.04)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                  }}
                >
                  <FileSpreadsheet size={18} color={r.isEnabled ? 'var(--primary)' : 'var(--text-muted)'} />
                </div>
                <div>
                  <div style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>{r.name}</div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    Process: <strong style={{ color: '#ffffff' }}>{r.processName}</strong>
                    {' • '} Type: {r.outputType}
                    {' • '} Pattern: {r.fileNamePattern || 'all'}
                    {' • '} Deliver to: {deliveryMethodLabel(r.deliveryMethod)}
                  </div>
                  {r.deliverOnFailure && (
                    <span style={{
                      fontSize: '10px', color: '#fbbf24',
                      background: 'rgba(251, 191, 36, 0.15)',
                      padding: '2px 6px', borderRadius: '4px', fontWeight: 600,
                      display: 'inline-block', marginTop: '4px',
                    }}>On Failure Too</span>
                  )}
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <button
                  onClick={() => onToggleRule(r.id!, !r.isEnabled)}
                  style={{
                    padding: '6px 14px', borderRadius: 'var(--radius-sm)',
                    background: r.isEnabled ? 'rgba(59, 130, 246, 0.15)' : 'rgba(255, 255, 255, 0.05)',
                    border: `1px solid ${r.isEnabled ? 'rgba(59, 130, 246, 0.3)' : 'var(--border-subtle)'}`,
                    color: r.isEnabled ? 'var(--primary)' : 'var(--text-muted)',
                    fontSize: '12px', fontWeight: 600, cursor: 'pointer',
                  }}
                >
                  {r.isEnabled ? 'Active' : 'Paused'}
                </button>
                {r.id && (
                  <button
                    onClick={() => onDeleteRule(r.id!)}
                    style={{
                      background: 'transparent', border: 'none',
                      color: 'var(--text-muted)', cursor: 'pointer', padding: '6px',
                    }}
                    title="Delete Rule"
                  >
                    <Trash2 size={16} />
                  </button>
                )}
              </div>
            </div>
          ))}
          {rules.length === 0 && (
            <div style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
              No output delivery rules configured. Create one to auto-share Excel outputs with Business users.
            </div>
          )}
        </div>
      </div>

      {/* Delivery Log */}
      <div className="glass-panel" style={{ padding: '22px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h3 style={{ fontSize: '14px', fontWeight: 700, color: '#ffffff' }}>Delivery History</h3>
          <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{deliveryLogs.length} records</span>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {deliveryLogs.map((log) => {
            const statusColor = log.status === 'Delivered' ? 'var(--emerald)' :
                                log.status === 'Skipped' ? 'var(--amber)' : '#fb7185';
            return (
              <div
                key={log.id}
                style={{
                  padding: '10px 16px',
                  background: 'rgba(15, 23, 42, 0.4)',
                  border: '1px solid var(--border-subtle)',
                  borderRadius: 'var(--radius-sm)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  {log.status === 'Delivered' ? (
                    <CheckCircle2 size={14} color="var(--emerald)" />
                  ) : log.status === 'Skipped' ? (
                    <AlertTriangle size={14} color="var(--amber)" />
                  ) : (
                    <AlertTriangle size={14} color="var(--rose)" />
                  )}
                  <div>
                    <div style={{ fontSize: '12px', fontWeight: 600, color: '#ffffff' }}>
                      {log.fileName || log.processName}
                    </div>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                      {log.processName} • {log.message?.slice(0, 80)}
                    </div>
                  </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <span style={{
                    fontSize: '11px', fontWeight: 700,
                    color: statusColor, textTransform: 'uppercase',
                  }}>
                    {log.status}
                  </span>
                  <span style={{ fontSize: '10px', color: 'var(--text-muted)' }}>
                    {new Date(log.deliveredAt).toLocaleString()}
                  </span>
                </div>
              </div>
            );
          })}
          {deliveryLogs.length === 0 && (
            <div style={{ textAlign: 'center', padding: '24px', color: 'var(--text-muted)', fontSize: '12px' }}>
              No delivery records yet. Click "Run Delivery Now" to process pending outputs.
            </div>
          )}
        </div>
      </div>

      <BusinessOutputRuleForm
        isOpen={isFormOpen}
        onClose={() => setIsFormOpen(false)}
        onSave={onSaveRule}
        processes={processes}
      />
    </div>
  );
};
