import { useQuery } from '@tanstack/react-query';
import { getApplications, getLogs, getHealth } from '../api/client';
import { clsx } from 'clsx';
import { format } from 'date-fns';
import SeverityBadge from '../components/SeverityBadge';

export default function Dashboard() {
  const { data: applications = [], isLoading: appsLoading } = useQuery({
    queryKey: ['applications'],
    queryFn: getApplications,
  });

  const { data: health } = useQuery({
    queryKey: ['health'],
    queryFn: getHealth,
    refetchInterval: 30000,
  });

  const { data: recentLogs } = useQuery({
    queryKey: ['logs', 'recent'],
    queryFn: () =>
      getLogs({
        severity: 'ERROR',
        limit: 10,
        startTime: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
      }),
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <div className="flex items-center space-x-2 text-sm">
          <span
            className={clsx(
              'flex items-center px-3 py-1 rounded-full',
              health?.status === 'Healthy'
                ? 'bg-green-900/50 text-green-400'
                : 'bg-red-900/50 text-red-400'
            )}
          >
            <span
              className={clsx(
                'w-2 h-2 rounded-full mr-2',
                health?.status === 'Healthy' ? 'bg-green-400' : 'bg-red-400'
              )}
            />
            {health?.status || 'Unknown'}
          </span>
        </div>
      </div>

      {/* Application Cards */}
      <div>
        <h2 className="text-lg font-semibold text-white mb-4">Applications</h2>
        {appsLoading ? (
          <div className="text-gray-400">Loading applications...</div>
        ) : applications.length === 0 ? (
          <div className="bg-gray-800 rounded-lg p-8 text-center">
            <p className="text-gray-400 mb-4">No applications registered yet.</p>
            <a
              href="/applications"
              className="text-blue-400 hover:text-blue-300"
            >
              Register your first application →
            </a>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {applications.map((app) => (
              <div
                key={app.id}
                className="bg-gray-800 rounded-lg p-4 border border-gray-700 hover:border-gray-600 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <h3 className="font-medium text-white">{app.name}</h3>
                    <p className="text-sm text-gray-400">{app.id}</p>
                  </div>
                  <span
                    className={clsx(
                      'px-2 py-1 text-xs rounded-full',
                      app.isActive
                        ? 'bg-green-900/50 text-green-400'
                        : 'bg-gray-700 text-gray-400'
                    )}
                  >
                    {app.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <div className="mt-4 text-sm text-gray-400">
                  <div>Environment: {app.environment || 'N/A'}</div>
                  <div>
                    Last seen:{' '}
                    {app.lastSeenAt
                      ? format(new Date(app.lastSeenAt), 'MMM d, HH:mm')
                      : 'Never'}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Recent Errors */}
      <div>
        <h2 className="text-lg font-semibold text-white mb-4">
          Recent Errors (Last 24h)
        </h2>
        <div className="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden">
          {!recentLogs?.data?.length ? (
            <div className="p-8 text-center text-gray-400">
              No errors in the last 24 hours
            </div>
          ) : (
            <table className="w-full">
              <thead className="bg-gray-900">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                    Time
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                    Severity
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                    Application
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                    Message
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-700">
                {recentLogs.data.map((log) => (
                  <tr key={log.id} className="hover:bg-gray-700/50">
                    <td className="px-4 py-3 text-sm text-gray-300 whitespace-nowrap">
                      {format(new Date(log.timestamp), 'HH:mm:ss')}
                    </td>
                    <td className="px-4 py-3">
                      <SeverityBadge severity={log.severity} />
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-300">
                      {log.applicationName}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-300 truncate max-w-md">
                      {log.message}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
