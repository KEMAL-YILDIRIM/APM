import { useState } from 'react';
import { clsx } from 'clsx';

const presets = [
  { label: '15m', value: 15 },
  { label: '1h', value: 60 },
  { label: '6h', value: 360 },
  { label: '24h', value: 1440 },
  { label: '7d', value: 10080 },
];

interface TimeRangeSelectorProps {
  value: number;
  onChange: (minutes: number) => void;
}

export default function TimeRangeSelector({ value, onChange }: TimeRangeSelectorProps) {
  return (
    <div className="flex items-center space-x-1 bg-gray-800 rounded-lg p-1">
      {presets.map((preset) => (
        <button
          key={preset.label}
          onClick={() => onChange(preset.value)}
          className={clsx(
            'px-3 py-1.5 text-sm font-medium rounded-md transition-colors',
            value === preset.value
              ? 'bg-blue-600 text-white'
              : 'text-gray-400 hover:text-white hover:bg-gray-700'
          )}
        >
          {preset.label}
        </button>
      ))}
    </div>
  );
}
