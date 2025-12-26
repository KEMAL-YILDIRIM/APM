import { clsx } from 'clsx';

const severityColors: Record<string, string> = {
  TRACE: 'bg-gray-500 text-gray-100',
  DEBUG: 'bg-blue-500 text-blue-100',
  INFO: 'bg-green-500 text-green-100',
  WARN: 'bg-yellow-500 text-yellow-900',
  WARNING: 'bg-yellow-500 text-yellow-900',
  ERROR: 'bg-red-500 text-red-100',
  FATAL: 'bg-purple-600 text-purple-100',
};

interface SeverityBadgeProps {
  severity: string;
}

export default function SeverityBadge({ severity }: SeverityBadgeProps) {
  const colorClass = severityColors[severity.toUpperCase()] || severityColors.INFO;

  return (
    <span
      className={clsx(
        'inline-flex items-center px-2 py-0.5 rounded text-xs font-medium',
        colorClass
      )}
    >
      {severity}
    </span>
  );
}
