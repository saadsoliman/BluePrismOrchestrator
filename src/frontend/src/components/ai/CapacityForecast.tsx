import React from 'react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  Legend,
} from 'recharts';
import { TrendingUp } from 'lucide-react';
import { CapacityForecastDto } from '../../types';

interface CapacityForecastProps {
  data: CapacityForecastDto[];
}

export const CapacityForecast: React.FC<CapacityForecastProps> = ({ data }) => {
  const chartData = data.map((d) => ({
    time: new Date(d.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    'Actual Utilization': d.actualUtilization > 0 ? d.actualUtilization : null,
    'Predicted Utilization': d.predictedUtilization,
    'Predicted Demand': d.predictedDemand,
  }));

  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <TrendingUp size={16} color="var(--cyan)" />
            Capacity Demand Forecast (Past 12h & Next 12h)
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            ML regression model forecasting bot utilization peaks and scaling needs
          </p>
        </div>
      </div>

      <div style={{ width: '100%', height: '300px' }}>
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={chartData} margin={{ top: 10, right: 20, left: -10, bottom: 0 }}>
            <defs>
              <linearGradient id="colorActual" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.4} />
                <stop offset="95%" stopColor="#3b82f6" stopOpacity={0.0} />
              </linearGradient>
              <linearGradient id="colorPredicted" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.3} />
                <stop offset="95%" stopColor="#06b6d4" stopOpacity={0.0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255, 255, 255, 0.06)" />
            <XAxis dataKey="time" stroke="#64748b" fontSize={11} />
            <YAxis stroke="#64748b" fontSize={11} domain={[0, 100]} unit="%" />
            <Tooltip
              contentStyle={{
                backgroundColor: 'rgba(15, 23, 42, 0.95)',
                border: '1px solid rgba(59, 130, 246, 0.3)',
                borderRadius: '8px',
                fontSize: '12px',
              }}
            />
            <Legend wrapperStyle={{ fontSize: '12px', paddingTop: '10px' }} />
            <Area
              type="monotone"
              dataKey="Actual Utilization"
              stroke="#3b82f6"
              strokeWidth={2}
              fillOpacity={1}
              fill="url(#colorActual)"
            />
            <Area
              type="monotone"
              dataKey="Predicted Utilization"
              stroke="#06b6d4"
              strokeWidth={2}
              strokeDasharray="4 4"
              fillOpacity={1}
              fill="url(#colorPredicted)"
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
