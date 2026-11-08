import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
import { authApi } from '../lib/api.js';
import { extractToken, saveToken } from '../lib/authStorage.js';

export default function AcquaintancePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);

  async function complete(event) {
    event.preventDefault();
    if (busy) return;
    if (state?.registration && !name.trim()) return window.alert('Введите имя, чтобы зарегистрироваться.');
    setBusy(true);
    try {
      if (state?.registration) {
        const result = await authApi.register({ ...state.registration, name: name.trim() });
        const token = extractToken(result);
        if (!token) {
          navigate('/login', { replace: true, state: { email: state.registration.email } });
          return;
        }
        saveToken(token);
      }
      if (name.trim()) {
        sessionStorage.setItem('ttravel_user_name', name.trim());
      }
      navigate('/welcome', { replace: true });
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant="acquaintance">
    <PageHeading title="Как к вам обращаться?">Это имя увидят друзья в совместных поездках.</PageHeading>
    <form className="acquaintance-form" onSubmit={complete}>
      <FormField label="Ваше имя" autoComplete="given-name" placeholder="Например, Алексей"
        value={name} onChange={e => setName(e.target.value)} />
      <CenterActions className="acquaintance-actions">
        <ActionButton type="submit" disabled={busy}>Продолжить</ActionButton>
        <ActionButton variant="secondary" disabled={busy} onClick={() => state?.registration
          ? window.alert('Для регистрации нужно указать имя.')
          : navigate('/welcome', { replace: true })}>Заполнить позже</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
