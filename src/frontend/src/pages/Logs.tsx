import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getLogs, getApplications, type LogEntry } from '../api/client';
import { format } from 'date-fns';
import { clsx } from 'clsx';
import SeverityBadge from '../components/SeverityBadge';
import TimeRangeSelector from '../components/TimeRangeSelector';

const severityOptions = ['ALL', 'TRACE', 'DEBUG', 'INFO', 'WARN', 'ERROR', 'FATAL'];

export default function Logs() {
  const [timeRange, setTimeRange] = useState(60); // minutes
  const [selectedApp, setSelectedApp] = useState<string>('');
  const [selectedSeverity, setSelectedSeverity] = useState<string>('ALL');
  const [search, setSearch] = useState('');
  const [expandedLog, setExpandedLog] = useState<string | null>(null);

  const startTime = new Date(Date.now() - timeRange * 60 * 1000).toISOString();

  const { data: applications = [] } = useQuery({
    queryKey: ['applications'],
    queryFn: getApplications,
  });

  const { data: logsData, isLoading, refetch } = useQuery({
    queryKey: ['logs', startTime, selectedApp, selectedSeverity, search],
    queryFn: () =>
      getLogs({
        startTime,
        applicationId: selectedApp || undefined,
        severity: selectedSeverity !== 'ALL' ? selectedSeverity : undefined,
        search: search || undefined,
        limit: 200,
      }),
    refetchInterval: 10000,
  });

  const logs = logsData?.data || [];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Logs</h1>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          Refresh
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-4 bg-gray-800 rounded-lg p-4">
        <TimeRangeSelector value={timeRange} onChange={setTimeRange} />

        <select
          value={selectedApp}
          onChange={(e) => setSelectedApp(e.target.value)}
          className="bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
        >
          <option value="">All Applications</option>
          {applications.map((app) => (
            <option key={app.id} value={app.id}>
              {app.name}
            </option>
          ))}
        </select>

        <select
          value={selectedSeverity}
          onChange={(e) => setSelectedSeverity(e.target.value)}
          className="bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
        >
          {severityOptions.map((sev) => (
            <option key={sev} value={sev}>
              {sev}
            </option>
          ))}
        </select>

        <input
          type="text"
          placeholder="Search logs..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="flex-1 min-w-[200px] bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
        />
      </div>

      {/* Results count */}
      <div className="text-sm text-gray-400">
        {isLoading ? 'Loading...' : `${logs.length} logs found`}
      </div>

      {/* Log list */}
      <div className="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden">
        {logs.length === 0 ? (
          <div className="p-8 text-center text-gray-400">
            No logs found matching your criteria
          </div>
        ) : (
          <div className="divide-y divide-gray-700">
            {logs.map((log) => (
              <LogRow
                key={log.id}
                log={log}
                expanded={expandedLog === log.id}
                onToggle={() =>
                  setExpandedLog(expandedLog === log.id ? null : log.id)
                }
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

interface LogRowProps {
  log: LogEntry;
  expanded: boolean;
  onToggle: () => void;
}

function LogRow({ log, expanded, onToggle }: LogRowProps) {
  return (
    <div>
      <div
        onClick={onToggle}
        className="flex items-center px-4 py-3 hover:bg-gray-700/50 cursor-pointer"
      >
        <span className="text-gray-500 mr-2">{expanded ? '▾' : '▸'}</span>
        <span className="text-sm text-gray-400 w-20 flex-shrink-0">
          {format(new Date(log.timestamp), 'HH:mm:ss')}
        </span>
        <span className="mr-3">
          <SeverityBadge severity={log.severity} />
        </span>
        <span className="text-sm text-gray-400 w-32 flex-shrink-0 truncate">
          {log.applicationName}
        </span>
        <span className="text-sm text-gray-200 flex-1 truncate">
          {log.message}
        </span>
      </div>
      {expanded && (
        <div className="px-4 py-3 bg-gray-900 border-t border-gray-700">
          <dl className="grid grid-cols-2 gap-2 text-sm">
            <dt className="text-gray-500">Timestamp</dt>
            <dd className="text-gray-300">
              {format(new Date(log.timestamp), 'yyyy-MM-dd HH:mm:ss.SSS')}
            </dd>
            <dt className="text-gray-500">Application</dt>
            <dd className="text-gray-300">{log.applicationName}</dd>
            {log.serviceName && (
              <>
                <dt className="text-gray-500">Service</dt>
                <dd className="text-gray-300">{log.serviceName}</dd>
              </>
            )}
            {log.traceId && (
              <>
                <dt className="text-gray-500">Trace ID</dt>
                <dd className="text-gray-300 font-mono text-xs">{log.traceId}</dd>
              </>
            )}
          </dl>
          <div className="mt-3">
            <div className="text-gray-500 text-sm mb-1">Message</div>
            <pre className="bg-gray-800 p-3 rounded text-sm text-gray-300 overflow-x-auto whitespace-pre-wrap">
              {log.message}
            </pre>
          </div>
          {log.attributes && Object.keys(log.attributes).length > 0 && (
            <div className="mt-3">
              <div className="text-gray-500 text-sm mb-1">Attributes</div>
              <pre className="bg-gray-800 p-3 rounded text-sm text-gray-300 overflow-x-auto">
                {JSON.stringify(log.attributes, null, 2)}
              </pre>
            </div>
          )}
          <div className="mt-3 flex space-x-2">
            <button
              onClick={() => navigator.clipboard.writeText(JSON.stringify(log, null, 2))}
              className="px-3 py-1 text-sm bg-gray-700 text-gray-300 rounded hover:bg-gray-600"
            >
              Copy JSON
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
