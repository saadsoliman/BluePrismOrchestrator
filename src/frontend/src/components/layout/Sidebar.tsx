import React from 'react';
import {
  LayoutDashboard,
  BrainCircuit,
  BarChart3,
  CalendarClock,
  BellRing,
  Cpu,
  Bot,
  Zap,
  Shield,
  FileSpreadsheet,
} from 'lucide-react';

export type ActiveTab = 'dashboard' | 'ai' | 'analytics' | 'schedules' | 'alerts' | 'operations';

interface SidebarProps {
  activeTab: ActiveTab;
  onTabChange: (tab: ActiveTab) => void;
  activeAlertCount?: number;
  activeRecommendationCount?: number;
}

export const Sidebar: React.FC<SidebarProps> = ({
  activeTab,
  onTabChange,
  activeAlertCount = 0,
  activeRecommendationCount = 0,
}) => {
  const navItems = [
    {
      id: 'dashboard' as ActiveTab,
      label: 'Live Operations',
      icon: LayoutDashboard,
      badge: null,
    },
    {
      id: 'ai' as ActiveTab,
      label: 'AI Insights & ML',
      icon: BrainCircuit,
      badge: activeRecommendationCount > 0 ? activeRecommendationCount : null,
      badgeColor: 'rgba(168, 85, 247, 0.25)',
      textColor: '#c084fc',
    },
    {
      id: 'analytics' as ActiveTab,
      label: 'Analytics & SLA',
      icon: BarChart3,
      badge: null,
    },
    {
      id: 'schedules' as ActiveTab,
      label: 'Smart Schedules',
      icon: CalendarClock,
      badge: null,
    },
    {
      id: 'alerts' as ActiveTab,
      label: 'Alerts & Rules',
      icon: BellRing,
      badge: activeAlertCount > 0 ? activeAlertCount : null,
      badgeColor: 'rgba(244, 63, 94, 0.25)',
      textColor: '#fb7185',
    },
    {
      id: 'operations' as ActiveTab,
      label: 'Cutoff & Outputs',
      icon: Shield,
      badge: null,
    },
  ];

  return (
    <aside
      style={{
        width: '260px',
        backgroundColor: 'var(--bg-sidebar)',
        borderRight: '1px solid var(--border-subtle)',
        display: 'flex',
        flexDirection: 'column',
        height: '100vh',
        position: 'sticky',
        top: 0,
        zIndex: 20,
      }}
    >
      {/* Brand Header */}
      <div
        style={{
          padding: '24px 20px',
          borderBottom: '1px solid var(--border-subtle)',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
        }}
      >
        <div
          style={{
            width: '40px',
            height: '40px',
            borderRadius: '10px',
            background: 'url(/images/adib-egypt-logo.png) no-repeat center center',
            backgroundSize: 'contain',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 16px rgba(59, 130, 246, 0.5)',
          }}
        />
        <div>
          <div
            style={{
              fontFamily: 'var(--font-display)',
              fontSize: '15px',
              fontWeight: 700,
              letterSpacing: '-0.01em',
              color: '#ffffff',
            }}
          >
            Blue Prism
          </div>
          <div
            style={{
              fontSize: '11px',
              color: 'var(--cyan)',
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.08em',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
            }}
          >
            <Zap size={10} /> Smart Orchestrator
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav style={{ padding: '20px 12px', flex: 1, display: 'flex', flexDirection: 'column', gap: '6px' }}>
        <div
          style={{
            fontSize: '10px',
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '0.1em',
            color: 'var(--text-muted)',
            padding: '4px 12px 8px',
          }}
        >
          Orchestration Layer
        </div>

        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = activeTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onTabChange(item.id)}
              style={{
                width: '100%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '10px 14px',
                borderRadius: 'var(--radius-sm)',
                background: isActive
                  ? 'linear-gradient(90deg, rgba(59, 130, 246, 0.18), rgba(6, 182, 212, 0.08))'
                  : 'transparent',
                border: isActive ? '1px solid rgba(59, 130, 246, 0.35)' : '1px solid transparent',
                color: isActive ? '#ffffff' : 'var(--text-secondary)',
                fontWeight: isActive ? 600 : 500,
                fontSize: '13px',
                cursor: 'pointer',
                textAlign: 'left',
                transition: 'all 0.2s ease',
              }}
              onMouseEnter={(e) => {
                if (!isActive) {
                  e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.04)';
                  e.currentTarget.style.color = '#ffffff';
                }
              }}
              onMouseLeave={(e) => {
                if (!isActive) {
                  e.currentTarget.style.backgroundColor = 'transparent';
                  e.currentTarget.style.color = 'var(--text-secondary)';
                }
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <Icon size={18} color={isActive ? 'var(--cyan)' : 'var(--text-muted)'} />
                <span>{item.label}</span>
              </div>
              {item.badge !== null && (
                <span
                  style={{
                    backgroundColor: item.badgeColor || 'rgba(59, 130, 246, 0.2)',
                    color: item.textColor || '#60a5fa',
                    padding: '2px 8px',
                    borderRadius: '9999px',
                    fontSize: '11px',
                    fontWeight: 700,
                  }}
                >
                  {item.badge}
                </span>
              )}
            </button>
          );
        })}
      </nav>

      {/* Footer / System Status */}
      <div
        style={{
          padding: '16px',
          margin: '12px',
          background: 'rgba(15, 23, 42, 0.8)',
          border: '1px solid var(--border-subtle)',
          borderRadius: 'var(--radius-md)',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
          <Cpu size={14} color="var(--primary-light)" />
          <span style={{ fontSize: '11px', fontWeight: 600, color: 'var(--text-secondary)' }}>AutomateC Bridge</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px' }}>
          <span style={{ color: 'var(--text-muted)' }}>CLI Channel:</span>
          <span style={{ color: 'var(--emerald)', fontWeight: 600 }}>Active</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', marginTop: '4px' }}>
          <span style={{ color: 'var(--text-muted)' }}>ML Engine:</span>
          <span style={{ color: 'var(--cyan)', fontWeight: 600 }}>Ready (ML.NET)</span>
        </div>
      </div>
    </aside>
  );
};
