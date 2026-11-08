import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { SvgStage, SvgInput, Hotspot } from '../components/SvgStage.jsx';
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
    } catch (e) { window.alert(e.message); }
    finally { setBusy(false); }
  }
  return <SvgStage name="Registration-1.svg" height={376}>
    <form className="svg-stage-form" onSubmit={submit}>
      <SvgInput x={32} y={142} w={446} label="Новый пароль" type={showPassword ? 'text' : 'password'}
        autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
      <Hotspot x={474} y={143} w={36} h={46} label={showPassword ? 'Скрыть пароль' : 'Показать пароль'} onClick={() => setShowPassword(!showPassword)} />
      <SvgInput x={32} y={232} w={446} label="Повторите пароль" type={showConfirm ? 'text' : 'password'}
        autoComplete="new-password" value={confirm} onChange={(e) => setConfirm(e.target.value)} />
      <Hotspot x={474} y={233} w={36} h={46} label={showConfirm ? 'Скрыть пароль' : 'Показать пароль'} onClick={() => setShowConfirm(!showConfirm)} />
      <Hotspot x={178} y={312} w={194} h={40} label="Обновить пароль" type="submit" disabled={busy} />
    </form>
  </SvgStage>;
}
