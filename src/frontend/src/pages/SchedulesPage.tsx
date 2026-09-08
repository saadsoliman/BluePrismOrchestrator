import React, { useState } from 'react';
import { ScheduleManager } from '../components/alerts/ScheduleManager';
import { NewScheduleModal } from '../components/alerts/NewScheduleModal';
import { ScheduleDto, ScheduleOptimizationResultDto } from '../types';

interface SchedulesPageProps {
  schedules: ScheduleDto[];
  onToggleSchedule: (schedule: ScheduleDto) => void;
  onDeleteSchedule: (id: string) => void;
  onSaveSchedule: (schedule: ScheduleDto) => void;
  onOptimize: () => void;
  optimizations: ScheduleOptimizationResultDto[];
  processes: string[];
  resources: string[];
}

export const SchedulesPage: React.FC<SchedulesPageProps> = ({
  schedules,
  onToggleSchedule,
  onDeleteSchedule,
  onSaveSchedule,
  onOptimize,
  optimizations,
  processes,
  resources,
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <div>
      <ScheduleManager
        schedules={schedules}
        onToggleSchedule={onToggleSchedule}
        onDeleteSchedule={onDeleteSchedule}
        onOptimize={onOptimize}
        optimizations={optimizations}
        onOpenCreateModal={() => setIsModalOpen(true)}
      />

      <NewScheduleModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={onSaveSchedule}
        processes={processes}
        resources={resources}
      />
    </div>
  );
};
