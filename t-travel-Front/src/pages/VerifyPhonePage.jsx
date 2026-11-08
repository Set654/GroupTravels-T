import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { SvgStage, Hotspot, SvgInput, OverlayText } from '../components/SvgStage.jsx';
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
    } catch (e) { window.alert(e.message); }
    finally { setBusy(false); }
  }
  return <SvgStage name="Confirm_code.svg" height={342}>
    {state?.phone && <OverlayText x={32} y={72} w={486} h={22}>Отправили код подтверждения на номер {state.phone}</OverlayText>}
    <form className="svg-stage-form" onSubmit={submit}>
      <SvgInput x={32} y={142} w={486} label="Код подтверждения" inputMode="numeric" maxLength={4} autoComplete="one-time-code"
        value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 4))} />
      <Hotspot x={198} y={246} w={154} h={40} type="submit" disabled={busy} label="Войти" />
    </form>
    <Hotspot x={320} y={290} w={91} h={31} label="Изменить номер" onClick={() => navigate('/login?method=sms')} />
  </SvgStage>;
}
