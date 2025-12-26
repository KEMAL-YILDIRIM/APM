import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import Logs from './pages/Logs';
import Metrics from './pages/Metrics';
import Applications from './pages/Applications';

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="logs" element={<Logs />} />
        <Route path="metrics" element={<Metrics />} />
        <Route path="applications" element={<Applications />} />
      </Route>
    </Routes>
  );
}

export default App;
