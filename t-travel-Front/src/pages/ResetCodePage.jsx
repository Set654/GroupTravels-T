import { useLocation, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { SvgStage, SvgInput, Hotspot } from '../components/SvgStage.jsx';
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
    } catch (e) { window.alert(e.message); }
    finally { setBusy(false); }
  }
  return <SvgStage name="Reset_password-1.svg" height={286}>
    <form className="svg-stage-form" onSubmit={submit}>
      <SvgInput x={32} y={142} w={486} label="Код сброса пароля" inputMode="numeric" maxLength={6}
        autoComplete="one-time-code" value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))} />
      <Hotspot x={178} y={222} w={194} h={40} type="submit" disabled={busy} label="Сбросить пароль" />
    </form>
  </SvgStage>;
}
