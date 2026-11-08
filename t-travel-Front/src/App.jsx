import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import LoginPage from './pages/LoginPage.jsx';
import RegisterPage from './pages/RegisterPage.jsx';
import AcquaintancePage from './pages/AcquaintancePage.jsx';
import VerifyPhonePage from './pages/VerifyPhonePage.jsx';
import ForgotPasswordPage from './pages/ForgotPasswordPage.jsx';
import ResetSentPage from './pages/ResetSentPage.jsx';
import ResetCodePage from './pages/ResetCodePage.jsx';
import NewPasswordPage from './pages/NewPasswordPage.jsx';
import WelcomePage from './pages/WelcomePage.jsx';
import DashboardPage from './pages/DashboardPage.jsx';
import { getToken } from './lib/authStorage.js';

function AuthLayout({ children }) {
  return <div className="auth-page-frame">{children}</div>;
}

function RequireAuth({ children }) {
  return getToken() ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<AuthLayout><LoginPage /></AuthLayout>} />
        <Route path="/register" element={<AuthLayout><RegisterPage /></AuthLayout>} />
        <Route path="/acquaintance" element={<AuthLayout><AcquaintancePage /></AuthLayout>} />
        <Route path="/verify-phone" element={<AuthLayout><VerifyPhonePage /></AuthLayout>} />
        <Route path="/forgot-password" element={<AuthLayout><ForgotPasswordPage /></AuthLayout>} />
        <Route path="/reset-sent" element={<AuthLayout><ResetSentPage /></AuthLayout>} />
        <Route path="/reset-code" element={<AuthLayout><ResetCodePage /></AuthLayout>} />
        <Route path="/new-password" element={<AuthLayout><NewPasswordPage /></AuthLayout>} />
        <Route path="/welcome" element={<RequireAuth><AuthLayout><WelcomePage /></AuthLayout></RequireAuth>} />
        <Route path="/dashboard" element={<RequireAuth><DashboardPage /></RequireAuth>} />
        {/* Создание поездки не реализовано: временно открываем дашборд. */}
        <Route path="/new" element={<RequireAuth><Navigate to="/dashboard" replace /></RequireAuth>} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
