import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getMetrics, getMetricNames, getApplications } from '../api/client';
import { format } from 'date-fns';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import TimeRangeSelector from '../components/TimeRangeSelector';

const aggregationOptions = ['none', 'avg', 'sum', 'min', 'max', 'count'];

export default function Metrics() {
  const [timeRange, setTimeRange] = useState(360); // 6 hours
  const [selectedApp, setSelectedApp] = useState<string>('');
  const [selectedMetric, setSelectedMetric] = useState<string>('');
  const [aggregation, setAggregation] = useState<string>('avg');

  const startTime = new Date(Date.now() - timeRange * 60 * 1000).toISOString();

  const { data: applications = [] } = useQuery({
    queryKey: ['applications'],
    queryFn: getApplications,
  });

  const { data: metricNames = [] } = useQuery({
    queryKey: ['metricNames', selectedApp],
    queryFn: () => getMetricNames(selectedApp || undefined),
  });

  const { data: metricsData, isLoading } = useQuery({
    queryKey: ['metrics', startTime, selectedApp, selectedMetric, aggregation],
    queryFn: () =>
      getMetrics({
        startTime,
        applicationId: selectedApp || undefined,
        name: selectedMetric || undefined,
        aggregation: aggregation !== 'none' ? aggregation : undefined,
        limit: 1000,
      }),
    enabled: !!selectedMetric,
  });

  const chartData = (metricsData?.data || [])
    .map((m) => ({
      timestamp: new Date(m.timestamp).getTime(),
      value: m.value,
      formattedTime: format(new Date(m.timestamp), 'HH:mm'),
    }))
    .sort((a, b) => a.timestamp - b.timestamp);

  const stats = chartData.length > 0
    ? {
        min: Math.min(...chartData.map((d) => d.value)),
        max: Math.max(...chartData.map((d) => d.value)),
        avg: chartData.reduce((sum, d) => sum + d.value, 0) / chartData.length,
      }
    : null;

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Metrics</h1>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-4 bg-gray-800 rounded-lg p-4">
        <TimeRangeSelector value={timeRange} onChange={setTimeRange} />

        <select
          value={selectedApp}
          onChange={(e) => {
            setSelectedApp(e.target.value);
            setSelectedMetric('');
          }}
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
          value={selectedMetric}
          onChange={(e) => setSelectedMetric(e.target.value)}
          className="bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none min-w-[200px]"
        >
          <option value="">Select a metric...</option>
          {metricNames.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>

        <select
          value={aggregation}
          onChange={(e) => setAggregation(e.target.value)}
          className="bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
        >
          {aggregationOptions.map((agg) => (
            <option key={agg} value={agg}>
              {agg === 'none' ? 'No aggregation' : agg.toUpperCase()}
            </option>
          ))}
        </select>
      </div>

      {/* Chart */}
      {!selectedMetric ? (
        <div className="bg-gray-800 rounded-lg p-8 text-center text-gray-400">
          Select a metric to view the chart
        </div>
      ) : isLoading ? (
        <div className="bg-gray-800 rounded-lg p-8 text-center text-gray-400">
          Loading metrics...
        </div>
      ) : chartData.length === 0 ? (
        <div className="bg-gray-800 rounded-lg p-8 text-center text-gray-400">
          No data available for the selected time range
        </div>
      ) : (
        <div className="space-y-4">
          <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
            <h3 className="text-lg font-medium text-white mb-4">
              {selectedMetric}
            </h3>
            <div className="h-80">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                  <XAxis
                    dataKey="formattedTime"
                    stroke="#9CA3AF"
                    fontSize={12}
                  />
                  <YAxis stroke="#9CA3AF" fontSize={12} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1F2937',
                      border: '1px solid #374151',
                      borderRadius: '8px',
                    }}
                    labelStyle={{ color: '#9CA3AF' }}
                    itemStyle={{ color: '#60A5FA' }}
                  />
                  <Line
                    type="monotone"
                    dataKey="value"
                    stroke="#60A5FA"
                    strokeWidth={2}
                    dot={false}
                    activeDot={{ r: 4 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Stats */}
          {stats && (
            <div className="grid grid-cols-3 gap-4">
              <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
                <div className="text-sm text-gray-400">Minimum</div>
                <div className="text-2xl font-bold text-white">
                  {stats.min.toFixed(2)}
                </div>
              </div>
              <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
                <div className="text-sm text-gray-400">Average</div>
                <div className="text-2xl font-bold text-white">
                  {stats.avg.toFixed(2)}
                </div>
              </div>
              <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
                <div className="text-sm text-gray-400">Maximum</div>
                <div className="text-2xl font-bold text-white">
                  {stats.max.toFixed(2)}
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
