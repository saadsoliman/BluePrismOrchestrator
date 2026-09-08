import React from 'react';
import { ProcessTrends } from '../components/analytics/ProcessTrends';
import { SLACompliance } from '../components/analytics/SLACompliance';
import { ResourceHeatmap } from '../components/analytics/ResourceHeatmap';
import {
  AnalyticsTrendDto,
  SlaComplianceDto,
  ResourceHeatmapDto,
} from '../types';

interface AnalyticsPageProps {
  trends: AnalyticsTrendDto | null;
  sla: SlaComplianceDto | null;
  heatmap: ResourceHeatmapDto[];
  processes: string[];
  selectedProcess: string;
  onSelectProcess: (process: string) => void;
}

export const AnalyticsPage: React.FC<AnalyticsPageProps> = ({
  trends,
  sla,
  heatmap,
  processes,
  selectedProcess,
  onSelectProcess,
}) => {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {/* 1. Process Historical Trends */}
      {trends && (
        <ProcessTrends
          trends={trends}
          processes={processes}
          selectedProcess={selectedProcess}
          onSelectProcess={onSelectProcess}
        />
      )}

      {/* 2. SLA Compliance & Resource Allocation Heatmap */}
      <div className="grid-equal-two">
        {sla && <SLACompliance data={sla} />}
        <ResourceHeatmap data={heatmap} />
      </div>
    </div>
  );
};
