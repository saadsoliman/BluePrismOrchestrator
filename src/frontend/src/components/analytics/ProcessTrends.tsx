import React, { useState } from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  Legend,
  BarChart,
  Bar,
} from 'recharts';
import { LineChart as LineChartIcon, BarChart2 } from 'lucide-react';
import { AnalyticsTrendDto } from '../../types';

interface ProcessTrendsProps {
  trends: AnalyticsTrendDto;
  processes: string[];
  selectedProcess: string;
  onSelectProcess: (process: string) => void;
}

export const ProcessTrends: React.FC<ProcessTrendsProps> = ({
  trends,
  processes,
  selectedProcess,
  onSelectProcess,
}) => {
  const [metricTab, setMetricTab] = useState<'duration' | 'success' | 'throughput'>('duration');

  const chartData = trends.durationTrend.map((d, idx) => ({
    date: new Date(d.timestamp).toLocaleDateString([], { month: 'short', day: 'numeric' }),
    'Duration (s)': d.value ?? null,
    'Success Rate (%)': trends.successRateTrend[idx]?.value ?? null,
    Throughput: trends.throughputTrend[idx]?.value ?? null,
  }));

  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '12px',
          marginBottom: '20px',
        }}
      >
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <LineChartIcon size={16} color="var(--primary)" />
            Process Performance Trends
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Historical duration, success stability, and volume throughput
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          {/* Process Selector */}
          <select
            value={selectedProcess}
            onChange={(e) => onSelectProcess(e.target.value)}
            className="input-field"
            style={{ width: 'auto', padding: '6px 12px', fontSize: '12px' }}
          >
            {processes.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>

          {/* Metric Selector Buttons */}
          <div
            style={{
              display: 'flex',
              background: 'rgba(255, 255, 255, 0.05)',
              padding: '2px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-subtle)',
            }}
          >
            <button
              onClick={() => setMetricTab('duration')}
              style={{
                padding: '5px 12px',
                borderRadius: '6px',
                background: metricTab === 'duration' ? 'var(--primary)' : 'transparent',
                color: metricTab === 'duration' ? '#ffffff' : 'var(--text-secondary)',
                border: 'none',
                fontSize: '11px',
                fontWeight: 600,
                cursor: 'pointer',
              }}
            >
              Avg Duration
            </button>
            <button
              onClick={() => setMetricTab('success')}
              style={{
                padding: '5px 12px',
                borderRadius: '6px',
                background: metricTab === 'success' ? 'var(--emerald)' : 'transparent',
                color: metricTab === 'success' ? '#ffffff' : 'var(--text-secondary)',
                border: 'none',
                fontSize: '11px',
                fontWeight: 600,
                cursor: 'pointer',
              }}
            >
              Success Rate
            </button>
            <button
              onClick={() => setMetricTab('throughput')}
              style={{
                padding: '5px 12px',
                borderRadius: '6px',
                background: metricTab === 'throughput' ? 'var(--purple)' : 'transparent',
                color: metricTab === 'throughput' ? '#ffffff' : 'var(--text-secondary)',
                border: 'none',
                fontSize: '11px',
                fontWeight: 600,
                cursor: 'pointer',
              }}
            >
              Throughput
            </button>
          </div>
        </div>
      </div>

          <div style={{ width: '100%', height: '300px' }}>
         <ResponsiveContainer width="100%" height="100%">
           {metricTab === 'throughput' ? (
             <BarChart data={chartData} margin={{ top: 10, right: 20, left: -10, bottom: 0 }}>
               <CartesianGrid strokeDasharray="3 3" stroke="rgba(255, 255, 255, 0.06)" />
               <XAxis dataKey="date" stroke="#64748b" fontSize={11} />
               <YAxis stroke="#64748b" fontSize={11} />
               <Tooltip
                 contentStyle={{
                   backgroundColor: 'rgba(15, 23, 42, 0.95)',
                   border: '1px solid rgba(168, 85, 247, 0.3)',
                   borderRadius: '8px',
                   fontSize: '12px',
                 }}
               />
               <Legend wrapperStyle={{ fontSize: '12px' }} />
               <Bar dataKey="Throughput" fill="#a855f7" radius={[4, 4, 0, 0]} />
             </BarChart>
           ) : (
             <LineChart data={chartData} margin={{ top: 10, right: 20, left: -10, bottom: 0 }}>
               <CartesianGrid strokeDasharray="3 3" stroke="rgba(255, 255, 255, 0.06)" />
               <XAxis dataKey="date" stroke="#64748b" fontSize={11} />
               <YAxis
                 stroke="#64748b"
                 fontSize={11}
                 unit={metricTab === 'success' ? '%' : 's'}
                 domain={metricTab === 'success' ? [80, 100] : ['auto', 'auto']}
               />
               <Tooltip
                 contentStyle={{
                   backgroundColor: 'rgba(15, 23, 42, 0.95)',
                   border: '1px solid rgba(59, 130, 246, 0.3)',
                   borderRadius: '8px',
                   fontSize: '12px',
                 }}
               />
               <Legend wrapperStyle={{ fontSize: '12px' }} />
               {metricTab === 'duration' ? (
                 <Line
                   type="monotone"
                   dataKey="Duration (s)"
                   stroke="#3b82f6"
                   strokeWidth={3}
                   dot={{ r: 4, fill: '#3b82f6' }}
                   activeDot={{ r: 6 }}
                   connectNulls
                 />
               ) : (
                 <Line
                   type="monotone"
                   dataKey="Success Rate (%)"
                   stroke="#10b981"
                   strokeWidth={3}
                   dot={{ r: 4, fill: '#10b981' }}
                   activeDot={{ r: 6 }}
                   connectNulls
                 />
               )}
             </LineChart>
           )}
         </ResponsiveContainer>
       </div>
    </div>
  );
};
