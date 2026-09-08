import React from 'react';
import { Sparkles, Check, X, ArrowUpRight, TrendingUp, AlertOctagon, Clock, Layers } from 'lucide-react';
import { AIRecommendationDto, RecommendationType } from '../../types';

interface RecommendationCardProps {
  recommendation: AIRecommendationDto;
  onAccept: (id: string) => void;
  onDismiss: (id: string) => void;
}

export const RecommendationCard: React.FC<RecommendationCardProps> = ({
  recommendation,
  onAccept,
  onDismiss,
}) => {
  const getIcon = (type: RecommendationType) => {
    switch (type) {
      case 'ScheduleOptimization':
        return Clock;
      case 'ResourceReassignment':
        return ArrowUpRight;
      case 'CapacityScaling':
        return TrendingUp;
      case 'FailurePrevention':
        return AlertOctagon;
      case 'QueueManagement':
        return Layers;
      default:
        return Sparkles;
    }
  };

  const Icon = getIcon(recommendation.type);

  return (
    <div
      className="glass-panel"
      style={{
        padding: '20px',
        border: '1px solid rgba(168, 85, 247, 0.3)',
        background: 'linear-gradient(135deg, rgba(168, 85, 247, 0.05) 0%, rgba(15, 23, 42, 0.7) 100%)',
        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.4)',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Top row: Type & Confidence */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div
            style={{
              width: '30px',
              height: '30px',
              borderRadius: '8px',
              background: 'rgba(168, 85, 247, 0.2)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Icon size={16} color="var(--purple)" />
          </div>
          <div>
            <span
              style={{
                fontSize: '11px',
                fontWeight: 700,
                color: 'var(--purple)',
                textTransform: 'uppercase',
                letterSpacing: '0.05em',
              }}
            >
              {recommendation.type.replace(/([A-Z])/g, ' $1').trim()}
            </span>
          </div>
        </div>

        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            background: 'rgba(59, 130, 246, 0.15)',
            border: '1px solid rgba(59, 130, 246, 0.3)',
            padding: '3px 8px',
            borderRadius: '9999px',
          }}
        >
          <Sparkles size={11} color="var(--primary)" />
          <span className="mono" style={{ fontSize: '11px', fontWeight: 700, color: 'var(--primary-light)' }}>
            {(recommendation.confidence * 100).toFixed(0)}% Confidence
          </span>
        </div>
      </div>

      {/* Title & Description */}
      <h4 style={{ fontSize: '15px', fontWeight: 700, color: '#ffffff', marginBottom: '6px' }}>
        {recommendation.title}
      </h4>
      <p style={{ fontSize: '13px', color: 'var(--text-secondary)', lineHeight: 1.5, marginBottom: '14px' }}>
        {recommendation.description}
      </p>

      {/* Impact Statement */}
      {recommendation.estimatedImpact && (
        <div
          style={{
            padding: '10px 12px',
            background: 'rgba(16, 185, 129, 0.1)',
            border: '1px solid rgba(16, 185, 129, 0.25)',
            borderRadius: 'var(--radius-sm)',
            fontSize: '12px',
            color: '#34d399',
            fontWeight: 500,
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            marginBottom: '16px',
          }}
        >
          <TrendingUp size={14} />
          <span>Estimated Impact: {recommendation.estimatedImpact}</span>
        </div>
      )}

      {/* Action Buttons */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
        <button
          onClick={() => onDismiss(recommendation.id)}
          className="btn-secondary"
          style={{ padding: '6px 12px', fontSize: '12px' }}
        >
          <X size={13} />
          <span>Dismiss</span>
        </button>
        <button
          onClick={() => onAccept(recommendation.id)}
          className="btn-primary"
          style={{
            background: 'linear-gradient(135deg, #7c3aed, #9333ea)',
            boxShadow: '0 2px 10px rgba(147, 51, 234, 0.4)',
            padding: '6px 14px',
            fontSize: '12px',
          }}
        >
          <Check size={13} />
          <span>Apply Recommendation</span>
        </button>
      </div>
    </div>
  );
};
