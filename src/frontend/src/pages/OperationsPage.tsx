import React, { useState } from 'react';
import { CutoffPolicyManager } from '../components/operations/CutoffPolicyManager';
import { BusinessOutputManager } from '../components/operations/BusinessOutputManager';
import { CutoffPolicyDto, BusinessOutputRuleDto, CutoffEventDto, BusinessOutputDeliveryLogDto } from '../types';

interface OperationsPageProps {
  cutoffPolicies: CutoffPolicyDto[];
  cutoffHistory: CutoffEventDto[];
  outputRules: BusinessOutputRuleDto[];
  outputLogs: BusinessOutputDeliveryLogDto[];
  processes: string[];
  onSaveCutoffPolicy: (policy: CutoffPolicyDto) => void;
  onUpdateCutoffPolicy: (id: string, policy: CutoffPolicyDto) => void;
  onDeleteCutoffPolicy: (id: string) => void;
  onToggleCutoffPolicy: (id: string, enabled: boolean) => void;
  onEvaluateCutoffs: () => void;
  onSaveOutputRule: (rule: BusinessOutputRuleDto) => void;
  onUpdateOutputRule: (id: string, rule: BusinessOutputRuleDto) => void;
  onDeleteOutputRule: (id: string) => void;
  onToggleOutputRule: (id: string, enabled: boolean) => void;
  onDeliverOutputs: () => void;
}

export const OperationsPage: React.FC<OperationsPageProps> = ({
  cutoffPolicies,
  cutoffHistory,
  outputRules,
  outputLogs,
  processes,
  onSaveCutoffPolicy,
  onUpdateCutoffPolicy,
  onDeleteCutoffPolicy,
  onToggleCutoffPolicy,
  onEvaluateCutoffs,
  onSaveOutputRule,
  onUpdateOutputRule,
  onDeleteOutputRule,
  onToggleOutputRule,
  onDeliverOutputs,
}) => {
  const [activeSection, setActiveSection] = useState<'cutoff' | 'output'>('cutoff');

  return (
    <div>
      <div style={{ marginBottom: '22px', display: 'flex', gap: '8px' }}>
        <button
          onClick={() => setActiveSection('cutoff')}
          style={{
            padding: '8px 18px',
            borderRadius: 'var(--radius-sm)',
            background: activeSection === 'cutoff' ? 'rgba(244, 63, 94, 0.15)' : 'rgba(255, 255, 255, 0.04)',
            border: `1px solid ${activeSection === 'cutoff' ? 'rgba(244, 63, 94, 0.4)' : 'var(--border-subtle)'}`,
            color: activeSection === 'cutoff' ? '#fb7185' : 'var(--text-secondary)',
            fontSize: '13px',
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          Cutoff Policies
        </button>
        <button
          onClick={() => setActiveSection('output')}
          style={{
            padding: '8px 18px',
            borderRadius: 'var(--radius-sm)',
            background: activeSection === 'output' ? 'rgba(59, 130, 246, 0.15)' : 'rgba(255, 255, 255, 0.04)',
            border: `1px solid ${activeSection === 'output' ? 'rgba(59, 130, 246, 0.4)' : 'var(--border-subtle)'}`,
            color: activeSection === 'output' ? 'var(--primary)' : 'var(--text-secondary)',
            fontSize: '13px',
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          Business Outputs
        </button>
      </div>

      {activeSection === 'cutoff' && (
        <CutoffPolicyManager
          policies={cutoffPolicies}
          history={cutoffHistory}
          processes={processes}
          onSavePolicy={onSaveCutoffPolicy}
          onUpdatePolicy={onUpdateCutoffPolicy}
          onDeletePolicy={onDeleteCutoffPolicy}
          onTogglePolicy={onToggleCutoffPolicy}
          onEvaluate={onEvaluateCutoffs}
        />
      )}

      {activeSection === 'output' && (
        <BusinessOutputManager
          rules={outputRules}
          deliveryLogs={outputLogs}
          processes={processes}
          onSaveRule={onSaveOutputRule}
          onUpdateRule={onUpdateOutputRule}
          onDeleteRule={onDeleteOutputRule}
          onToggleRule={onToggleOutputRule}
          onDeliver={onDeliverOutputs}
        />
      )}
    </div>
  );
};
