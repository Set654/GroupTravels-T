import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { SvgStage, Hotspot, SvgInput } from '../components/SvgStage.jsx';
import { authApi } from '../lib/api.js';
import { extractToken, saveToken } from '../lib/authStorage.js';

export default function AcquaintancePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const [name, setName] = useState('');
  const [busy, setBusy] = useState(false);
  
async function complete(event) {
  event?.preventDefault();

  if (busy) return;

  if (state?.registration && !name.trim()) {
    return window.alert('Введите имя, чтобы зарегистрироваться.');
  }

  setBusy(true);

  try {
    if (state?.registration) {
      const result = await authApi.register({
        ...state.registration,
        name: name.trim()
      });

      const token = extractToken(result);

      if (!token) {
        navigate('/login', {
          replace: true,
          state: { email: state.registration.email }
        });
        return;
      }

      saveToken(token);
    }
    if (name.trim()) {
      sessionStorage.setItem('ttravel_user_name', name.trim());
    }

    navigate('/welcome', { replace: true });

  } catch (e) {
    window.alert(e.message);
  } finally {
    setBusy(false);
  }
}
  return <SvgStage name="Acquaintance.svg" height={330}>
    <form className="svg-stage-form" onSubmit={complete}>
      <SvgInput x={32} y={142} w={486} label="Ваше имя" autoComplete="given-name" value={name} onChange={(e) => setName(e.target.value)} />
      <Hotspot x={178} y={222} w={194} h={40} type="submit" disabled={busy} label="Продолжить" />
    </form>
    <Hotspot x={178} y={266} w={194} h={40} disabled={busy} label="Заполнить позже"
      onClick={() => state?.registration ? window.alert('Для регистрации нужно указать имя.') : navigate('/welcome', { replace: true })} />
  </SvgStage>;
}
