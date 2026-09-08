import React, { useState } from 'react';
import { Sparkles, RefreshCw, Cpu, CheckCircle } from 'lucide-react';
import { RecommendationCard } from '../components/ai/RecommendationCard';
import { FailureHeatmap } from '../components/ai/FailureHeatmap';
import { CapacityForecast } from '../components/ai/CapacityForecast';
import { AnomalyFeed } from '../components/ai/AnomalyFeed';
import { LLMModelSelector } from '../components/ai/LLMModelSelector';
import {
  AIRecommendationDto,
  FailurePredictionDto,
  CapacityForecastDto,
  AnomalyDto,
} from '../types';

interface AIInsightsPageProps {
  recommendations: AIRecommendationDto[];
  heatmap: FailurePredictionDto[];
  forecast: CapacityForecastDto[];
  anomalies: AnomalyDto[];
  onAcceptRecommendation: (id: string) => void;
  onDismissRecommendation: (id: string) => void;
  onRetrain: () => Promise<void>;
}

export const AIInsightsPage: React.FC<AIInsightsPageProps> = ({
  recommendations,
  heatmap,
  forecast,
  anomalies,
  onAcceptRecommendation,
  onDismissRecommendation,
  onRetrain,
}) => {
  const [retraining, setRetraining] = useState(false);
  const [retrainedToast, setRetrainedToast] = useState(false);

  const handleRetrain = async () => {
    setRetraining(true);
    await onRetrain();
    setRetraining(false);
    setRetrainedToast(true);
    setTimeout(() => setRetrainedToast(false), 4000);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {/* Header Banner */}
      <div
        className="glass-panel"
        style={{
          padding: '24px',
          background: 'linear-gradient(135deg, rgba(168, 85, 247, 0.12) 0%, rgba(59, 130, 246, 0.08) 100%)',
          border: '1px solid rgba(168, 85, 247, 0.3)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '16px',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '6px' }}>
            <div
              style={{
                width: '32px',
                height: '32px',
                borderRadius: '8px',
                background: 'rgba(168, 85, 247, 0.25)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Cpu size={18} color="var(--purple)" />
            </div>
            <h2 style={{ fontSize: '18px', fontWeight: 800, color: '#ffffff' }}>
              Autonomous AI Telemetry & Neural Recommendations
            </h2>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
            Trained on historical Blue Prism execution sessions, exception reasons, and queue backlog dynamics.
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {retrainedToast && (
            <span style={{ fontSize: '12px', color: 'var(--emerald)', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <CheckCircle size={14} /> ML Models Updated
            </span>
          )}
          <button
            onClick={handleRetrain}
            disabled={retraining}
            className="btn-primary"
            style={{ background: 'linear-gradient(135deg, #7c3aed, #9333ea)' }}
          >
            <RefreshCw size={14} className={retraining ? 'animate-spin' : ''} />
            <span>{retraining ? 'Fitting Models...' : 'Retrain ML Models'}</span>
          </button>
        </div>
      </div>

      {/* 1. Actionable AI Recommendations */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '14px' }}>
          <Sparkles size={16} color="var(--purple)" />
          <h3 style={{ fontSize: '15px', fontWeight: 700 }}>
            Active Optimization Recommendations ({recommendations.length})
          </h3>
        </div>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))',
            gap: '16px',
          }}
        >
          {recommendations.map((rec) => (
            <RecommendationCard
              key={rec.id}
              recommendation={rec}
              onAccept={onAcceptRecommendation}
              onDismiss={onDismissRecommendation}
            />
          ))}
        </div>
      </div>

      {/* 2. Failure Risk Heatmap */}
      <FailureHeatmap data={heatmap} />

      {/* 3. Capacity Demand Forecast & Anomaly Stream */}
      <div className="grid-equal-two">
        <CapacityForecast data={forecast} />
        <AnomalyFeed anomalies={anomalies} />
      </div>

      {/* 4. LLM Model Selector */}
      <LLMModelSelector />
    </div>
  );
};
