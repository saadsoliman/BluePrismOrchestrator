import React, { useState, useEffect } from 'react';
import { Server, HardDrive, Globe } from 'lucide-react';
import { api } from '../../api/client';
import { LLMConfigDto } from '../../types';

export const LLMModelSelector: React.FC = () => {
  const [config, setConfig] = useState<LLMConfigDto | null>(null);
  const [selectedModel, setSelectedModel] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api.getLLMConfig().then(setConfig).catch(() => {});
  }, []);

  const handleSelect = async (modelId: string) => {
    setSelectedModel(modelId);
    setLoading(true);
    setMessage(null);
    try {
      const result = await api.setActiveModel(modelId);
      setMessage(result.message);
    } catch (err: any) {
      setMessage(err.message || 'Failed to set model');
    } finally {
      setLoading(false);
    }
  };

  if (!config) {
    return (
      <div className="glass-panel" style={{ padding: '20px' }}>
        <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Loading LLM models...</div>
      </div>
    );
  }

  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Server size={16} color="var(--primary)" />
            LLM Model Selector
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Choose between cloud providers, local LLMs (Ollama/LM Studio), or external endpoints
          </p>
        </div>
        <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
          Current: <strong style={{ color: '#ffffff' }}>{config.currentModel}</strong> ({config.currentProvider})
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        {config.models.map((model) => {
          const isSelected = selectedModel === model.id || model.id === config.currentModel;
          const icon = model.isLocal ? <HardDrive size={14} color="var(--emerald)" /> :
                       model.provider === 'AzureOpenAI' ? <Globe size={14} color="var(--cyan)" /> :
                       model.provider === 'Anthropic' ? <Globe size={14} color="var(--rose)" /> :
                       <Globe size={14} color="var(--primary)" />;

          const typeLabel = model.isLocal ? 'Local' : model.isCustom ? 'Custom External' : 'Cloud Provider';

          return (
            <div
              key={model.id}
              style={{
                padding: '14px 16px',
                background: isSelected ? 'rgba(59, 130, 246, 0.08)' : 'rgba(15, 23, 42, 0.4)',
                border: `1px solid ${isSelected ? 'var(--primary)' : 'var(--border-subtle)'}`,
                borderRadius: 'var(--radius-sm)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                {icon}
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff' }}>{model.name}</span>
                    <span
                      style={{
                        fontSize: '10px',
                        color: model.isLocal ? 'var(--emerald)' : 'var(--text-muted)',
                        background: model.isLocal ? 'rgba(16, 185, 129, 0.15)' : 'rgba(255, 255, 255, 0.04)',
                        padding: '2px 6px',
                        borderRadius: '4px',
                      }}
                    >
                      {typeLabel}
                    </span>
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>
                    ID: {model.id}
                    {model.maxTokens && ` • Max tokens: ${model.maxTokens}`}
                    {model.url && ` • ${model.url}`}
                  </div>
                </div>
              </div>
              <button
                onClick={() => handleSelect(model.id)}
                disabled={loading || isSelected}
                style={{
                  padding: '6px 14px',
                  borderRadius: 'var(--radius-sm)',
                  background: isSelected ? 'rgba(16, 185, 129, 0.15)' : 'rgba(59, 130, 246, 0.15)',
                  border: `1px solid ${isSelected ? 'rgba(16, 185, 129, 0.3)' : 'var(--border-subtle)'}`,
                  color: isSelected ? 'var(--emerald)' : 'var(--primary)',
                  fontSize: '11px',
                  fontWeight: 600,
                  cursor: loading || isSelected ? 'default' : 'pointer',
                  minWidth: '80px',
                }}
              >
                {loading && selectedModel === model.id ? 'Setting...' : isSelected ? 'Selected' : 'Select'}
              </button>
            </div>
          );
        })}
      </div>

      {message && (
        <div style={{
          marginTop: '12px', padding: '10px 14px', borderRadius: 'var(--radius-sm)',
          background: message.includes('success') ? 'rgba(16, 185, 129, 0.15)' : 'rgba(244, 63, 94, 0.15)',
          border: `1px solid ${message.includes('success') ? 'rgba(16, 185, 129, 0.3)' : 'rgba(244, 63, 94, 0.3)'}`,
          fontSize: '11px', color: message.includes('success') ? 'var(--emerald)' : '#fb7185',
        }}>
          {message}
        </div>
      )}

      <div style={{ marginTop: '16px', padding: '10px 14px', background: 'rgba(15, 23, 42, 0.4)', borderRadius: 'var(--radius-sm)' }}>
        <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
          <strong style={{ color: '#ffffff' }}>Config Details:</strong>
          <div style={{ marginTop: '4px' }}>
            Provider: {config.currentProvider} • Base URL: {config.baseUrl}
          </div>
          <div style={{ marginTop: '2px' }}>
            Max Tokens: {config.maxTokens} • Temperature: {config.temperature}
          </div>
        </div>
      </div>
    </div>
  );
};
