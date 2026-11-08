import { useLocation, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { AuthCard, PageHeading, FormField, ActionButton, CenterActions } from '../components/AuthUI.jsx';
import { authApi } from '../lib/api.js';

export default function ResetCodePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event) {
    event.preventDefault();
    if (!state?.contact) return navigate('/forgot-password');
    if (code.length !== 6) return window.alert('Введите код из 6 цифр.');
    setBusy(true);
    try {
      const data = await authApi.verifyResetCode(state.contact, state.type, code);
      const resetToken = data?.resetToken ?? data?.token;
      if (!resetToken) throw new Error('Сервер не вернул токен сброса пароля.');
      navigate('/new-password', { state: { resetToken }, replace: true });
    } catch (error) {
      window.alert(error.message);
    } finally {
      setBusy(false);
    }
  }

  return <AuthCard variant="reset-code">
    <PageHeading title="Введите код">На ваш Email был отправлен код для сброса пароля.</PageHeading>
    <form className="reset-code-form" onSubmit={submit}>
      <FormField label="Код" inputMode="numeric" maxLength={6} autoComplete="one-time-code"
        placeholder="000000" value={code} onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} />
      <CenterActions className="reset-code-submit">
        <ActionButton type="submit" disabled={busy}>Сбросить пароль</ActionButton>
      </CenterActions>
    </form>
  </AuthCard>;
}
