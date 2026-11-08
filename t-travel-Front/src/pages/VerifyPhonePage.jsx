import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { AuthCard, PageHeading, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
import { authApi } from '../lib/api.js';
import { extractToken, saveToken } from '../lib/authStorage.js';

export default function VerifyPhonePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event) {
    event.preventDefault();
    if (!state?.phone) return navigate('/login?method=sms');
    if (code.length !== 4) return window.alert('Введите код из 4 цифр.');
    setBusy(true);
    try {
      const data = await authApi.verifyPhoneCode(state.phone, code);
      const token = extractToken(data);
      if (!token) throw new Error('Сервер не вернул JWT-токен.');
      saveToken(token);
      navigate('/welcome', { replace: true });
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant="verify">
    <PageHeading title="Введите код">Отправили код подтверждения на номер {state?.phone || '+7 800 555-35-35'}</PageHeading>
    <form className="verify-form" onSubmit={submit}>
      <FormField label="Код" inputMode="numeric" maxLength={4} autoComplete="one-time-code"
        placeholder="0000" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 4))} />
      <p className="small-hint">Отправим снова через 40 с.</p>
      <CenterActions className="verify-actions">
        <ActionButton type="submit" size="small" disabled={busy}>Войти</ActionButton>
      </CenterActions>
    </form>
    <div className="verify-change">Неправильный номер? <button type="button" className="text-link" onClick={() => navigate('/login?method=sms')}>Изменить</button></div>
  </AuthCard>;
}
