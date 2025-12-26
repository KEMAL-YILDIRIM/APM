import { Outlet, NavLink } from 'react-router-dom';
import { clsx } from 'clsx';

const navigation = [
  { name: 'Dashboard', href: '/dashboard', icon: '📊' },
  { name: 'Logs', href: '/logs', icon: '📝' },
  { name: 'Metrics', href: '/metrics', icon: '📈' },
  { name: 'Applications', href: '/applications', icon: '🖥️' },
];

export default function Layout() {
  return (
    <div className="min-h-screen bg-gray-900">
      {/* Sidebar */}
      <div className="fixed inset-y-0 left-0 w-64 bg-gray-800 border-r border-gray-700">
        <div className="flex items-center h-16 px-6 border-b border-gray-700">
          <span className="text-xl font-bold text-white">APM Dashboard</span>
        </div>
        <nav className="mt-6 px-3">
          {navigation.map((item) => (
            <NavLink
              key={item.name}
              to={item.href}
              className={({ isActive }) =>
                clsx(
                  'flex items-center px-4 py-3 mb-1 rounded-lg text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                )
              }
            >
              <span className="mr-3">{item.icon}</span>
              {item.name}
            </NavLink>
          ))}
        </nav>
      </div>

      {/* Main content */}
      <div className="pl-64">
        <header className="h-16 bg-gray-800 border-b border-gray-700 flex items-center justify-between px-6">
          <div className="text-sm text-gray-400">
            Application Performance Monitoring
          </div>
          <div className="flex items-center space-x-4">
            <span className="flex items-center text-sm text-green-400">
              <span className="w-2 h-2 bg-green-400 rounded-full mr-2"></span>
              Connected
            </span>
          </div>
        </header>
        <main className="p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
