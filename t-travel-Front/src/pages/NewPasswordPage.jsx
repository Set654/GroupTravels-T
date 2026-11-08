import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
import { authApi } from '../lib/api.js';
import { validatePassword } from '../lib/validators.js';

export default function NewPasswordPage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [busy, setBusy] = useState(false);

  async function submit(event) {
    event.preventDefault();
    if (!state?.resetToken) return navigate('/forgot-password');
    const error = validatePassword(password);
    if (error) return window.alert(error);
    if (password !== confirm) return window.alert('Пароли не совпадают.');
    setBusy(true);
    try {
      await authApi.updatePassword(state.resetToken, password);
      navigate('/login', { replace: true });
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant="new-password">
    <PageHeading title="Придумайте новый пароль">Новый пароль должен отличаться от старого.</PageHeading>
    <form className="new-password-form" onSubmit={submit}>
      <FormField label="Новый пароль" type={showPassword ? 'text' : 'password'} autoComplete="new-password"
        placeholder="Не менее 8 символов" value={password} onChange={e => setPassword(e.target.value)}
        toggleVisible={showPassword} onToggleVisible={() => setShowPassword(v => !v)} />
      <FormField className="new-password-confirm" label="Повторите пароль" type={showConfirm ? 'text' : 'password'}
        autoComplete="new-password" placeholder="Повторите пароль" value={confirm} onChange={e => setConfirm(e.target.value)}
        toggleVisible={showConfirm} onToggleVisible={() => setShowConfirm(v => !v)} />
      <CenterActions className="new-password-submit">
        <ActionButton type="submit" disabled={busy}>Обновить пароль</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
