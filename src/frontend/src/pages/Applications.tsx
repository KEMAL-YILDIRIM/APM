import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getApplications, createApplication, regenerateApiKey } from '../api/client';
import { format } from 'date-fns';
import { clsx } from 'clsx';

export default function Applications() {
  const queryClient = useQueryClient();
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newAppName, setNewAppName] = useState('');
  const [newAppEnv, setNewAppEnv] = useState('development');
  const [createdApiKey, setCreatedApiKey] = useState<string | null>(null);

  const { data: applications = [], isLoading } = useQuery({
    queryKey: ['applications'],
    queryFn: getApplications,
  });

  const createMutation = useMutation({
    mutationFn: createApplication,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      setCreatedApiKey(data.apiKey);
      setNewAppName('');
      setNewAppEnv('development');
    },
  });

  const regenerateMutation = useMutation({
    mutationFn: regenerateApiKey,
    onSuccess: (data) => {
      setCreatedApiKey(data.apiKey);
    },
  });

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate({ name: newAppName, environment: newAppEnv });
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Applications</h1>
        <button
          onClick={() => {
            setShowCreateForm(true);
            setCreatedApiKey(null);
          }}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          Register Application
        </button>
      </div>

      {/* Create Form Modal */}
      {showCreateForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-gray-800 rounded-lg p-6 w-full max-w-md border border-gray-700">
            <h2 className="text-xl font-bold text-white mb-4">
              Register New Application
            </h2>
            {createdApiKey ? (
              <div className="space-y-4">
                <div className="bg-green-900/30 border border-green-700 rounded-lg p-4">
                  <p className="text-green-400 font-medium mb-2">
                    Application created successfully!
                  </p>
                  <p className="text-sm text-gray-400 mb-2">
                    Save this API key - it won't be shown again:
                  </p>
                  <div className="bg-gray-900 p-3 rounded font-mono text-sm text-white break-all">
                    {createdApiKey}
                  </div>
                </div>
                <button
                  onClick={() => {
                    setShowCreateForm(false);
                    setCreatedApiKey(null);
                  }}
                  className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  Close
                </button>
              </div>
            ) : (
              <form onSubmit={handleCreate} className="space-y-4">
                <div>
                  <label className="block text-sm text-gray-400 mb-1">
                    Application Name
                  </label>
                  <input
                    type="text"
                    value={newAppName}
                    onChange={(e) => setNewAppName(e.target.value)}
                    required
                    className="w-full bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
                    placeholder="My Application"
                  />
                </div>
                <div>
                  <label className="block text-sm text-gray-400 mb-1">
                    Environment
                  </label>
                  <select
                    value={newAppEnv}
                    onChange={(e) => setNewAppEnv(e.target.value)}
                    className="w-full bg-gray-700 text-white px-3 py-2 rounded-lg border border-gray-600 focus:border-blue-500 focus:outline-none"
                  >
                    <option value="development">Development</option>
                    <option value="staging">Staging</option>
                    <option value="production">Production</option>
                  </select>
                </div>
                <div className="flex space-x-3">
                  <button
                    type="button"
                    onClick={() => setShowCreateForm(false)}
                    className="flex-1 px-4 py-2 bg-gray-700 text-gray-300 rounded-lg hover:bg-gray-600"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={createMutation.isPending}
                    className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  >
                    {createMutation.isPending ? 'Creating...' : 'Create'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}

      {/* Applications List */}
      {isLoading ? (
        <div className="text-gray-400">Loading applications...</div>
      ) : applications.length === 0 ? (
        <div className="bg-gray-800 rounded-lg p-8 text-center border border-gray-700">
          <p className="text-gray-400 mb-4">No applications registered yet.</p>
          <p className="text-sm text-gray-500">
            Click "Register Application" to get started.
          </p>
        </div>
      ) : (
        <div className="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-900">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  Name
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  ID
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  Environment
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  Last Seen
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-400 uppercase">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-700">
              {applications.map((app) => (
                <tr key={app.id} className="hover:bg-gray-700/50">
                  <td className="px-4 py-3 text-sm text-white font-medium">
                    {app.name}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-400 font-mono">
                    {app.id}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-400">
                    {app.environment || 'N/A'}
                  </td>
                  <td className="px-4 py-3">
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
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-400">
                    {app.lastSeenAt
                      ? format(new Date(app.lastSeenAt), 'MMM d, HH:mm')
                      : 'Never'}
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => {
                        if (
                          confirm(
                            'This will invalidate the current API key. Continue?'
                          )
                        ) {
                          regenerateMutation.mutate(app.id);
                          setShowCreateForm(true);
                        }
                      }}
                      className="text-sm text-blue-400 hover:text-blue-300"
                    >
                      Regenerate Key
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
