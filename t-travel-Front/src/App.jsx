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

function RequireAuth({ children }) {
  return getToken() ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return <BrowserRouter><Routes>
    <Route path="/" element={<Navigate to="/login" replace />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route path="/acquaintance" element={<AcquaintancePage />} />
    <Route path="/verify-phone" element={<VerifyPhonePage />} />
    <Route path="/forgot-password" element={<ForgotPasswordPage />} />
    <Route path="/reset-sent" element={<ResetSentPage />} />
    <Route path="/reset-code" element={<ResetCodePage />} />
    <Route path="/new-password" element={<NewPasswordPage />} />
    <Route path="/welcome" element={<RequireAuth><WelcomePage /></RequireAuth>} />
    <Route path="*" element={<Navigate to="/login" replace />} />
    <Route path="/dashboard"element={<RequireAuth><DashboardPage /></RequireAuth>}/>
  </Routes></BrowserRouter>;
}
