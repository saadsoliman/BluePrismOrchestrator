import React from 'react';
import { Cpu } from 'lucide-react';
import { ResourceHeatmapDto } from '../../types';

interface ResourceHeatmapProps {
  data: ResourceHeatmapDto[];
}

export const ResourceHeatmap: React.FC<ResourceHeatmapProps> = ({ data }) => {
  const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  const hours = [0, 3, 6, 9, 12, 15, 18, 21];

  const getColor = (util: number) => {
    if (util < 25) return 'rgba(59, 130, 246, 0.15)';
    if (util < 50) return 'rgba(6, 182, 212, 0.35)';
    if (util < 75) return 'rgba(245, 158, 11, 0.5)';
    return 'rgba(244, 63, 94, 0.7)';
  };

  return (
    <div className="glass-panel" style={{ padding: '22px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '18px' }}>
        <div>
          <h3 style={{ fontSize: '15px', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Cpu size={16} color="var(--primary)" />
            Resource Utilization Heatmap (Weekly Profile)
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            Bot hardware utilization across day-of-week and time slots
          </p>
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {data.map((res) => (
          <div key={res.resourceName} style={{ background: 'rgba(15, 23, 42, 0.4)', padding: '14px', borderRadius: 'var(--radius-sm)' }}>
            <div style={{ fontSize: '13px', fontWeight: 600, color: '#ffffff', marginBottom: '10px' }}>
              {res.resourceName}
            </div>

            <div style={{ overflowX: 'auto' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '50px repeat(8, 1fr)', gap: '6px', minWidth: '450px' }}>
                <div />
                {hours.map((h) => (
                  <div key={h} className="mono" style={{ fontSize: '10px', color: 'var(--text-muted)', textAlign: 'center' }}>
                    {String(h).padStart(2, '0')}:00
                  </div>
                ))}

                {days.map((dayName, dayIdx) => (
                  <React.Fragment key={dayName}>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'flex', alignItems: 'center' }}>
                      {dayName}
                    </div>
                    {hours.map((hour) => {
                      const cell = res.cells.find((c) => c.dayOfWeek === dayIdx && c.hour === hour);
                      const util = cell ? cell.utilizationPercent : 20;
                      return (
                        <div
                          key={hour}
                          style={{
                            height: '24px',
                            background: getColor(util),
                            borderRadius: '4px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '10px',
                            color: '#ffffff',
                            fontWeight: 600,
                          }}
                          title={`${dayName} ${hour}:00 — ${util.toFixed(0)}% utilization`}
                        >
                          {util.toFixed(0)}%
                        </div>
                      );
                    })}
                  </React.Fragment>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
